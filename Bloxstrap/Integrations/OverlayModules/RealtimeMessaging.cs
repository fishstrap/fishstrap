using System.Net.WebSockets;

using Bloxstrap.Models.APIs.RealtimeMessaging;
using Bloxstrap.Models.APIs.RobloxParty.Events;

namespace Bloxstrap.Integrations.OverlayModules
{
    public class RealtimeMessaging : IAsyncDisposable
    {
        private const string RecordSeparator = "";
        private const int PingType = 6;
        private const int PingIntervalMs = 10000;

        private readonly Uri _userhubUrl = new("wss://realtime-signalr.roblox.com/userhub");

        private ClientWebSocket? _webSocket;
        private SemaphoreSlim? _sendLock;
        private CancellationTokenSource? _cancellation;

        private Task? _receiveTask;
        private Task? _pingTask;

        public event EventHandler<MessageEvent>? PartyChat;

        public readonly RobloxParty Party;

        public bool IsConnected => _webSocket is not null && _webSocket.State == WebSocketState.Open;

        public RealtimeMessaging()
        {
            Party = new RobloxParty(this);
        }

        #region Connection

        public async void ConnectToUserhub()
        {
            const string LOG_IDENT = "RealtimeMessaging::ConnectToUserhub";

            if (!App.Settings.Prop.AllowCookieAccess)
            {
                App.Logger.WriteLine(LOG_IDENT, "Cookie access is off, not connecting");
                return;
            }

            if (_webSocket is not null)
                await DisconnectFromUserhub();

            _sendLock = new SemaphoreSlim(1, 1);
            _webSocket = new ClientWebSocket();
            _cancellation = new CancellationTokenSource();

            try
            {
                App.Cookies.AuthWebsocket(_webSocket);

                App.Logger.WriteLine(LOG_IDENT, "Connecting to userhub");

                await _webSocket.ConnectAsync(_userhubUrl, _cancellation.Token);

                App.Logger.WriteLine(LOG_IDENT, "Connected to userhub");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Unable to connect to userhub");
                App.Logger.WriteException(LOG_IDENT, ex);

                await DisconnectFromUserhub();
                return;
            }

            await SendHandshake();

            _receiveTask = ReceiveLoop(_cancellation.Token);
            _pingTask = PingLoop(_cancellation.Token);
        }

        public async Task DisconnectFromUserhub()
        {
            const string LOG_IDENT = "RealtimeMessaging::DisconnectFromUserhub";

            App.Logger.WriteLine(LOG_IDENT, "Disconnecting from userhub");

            _cancellation?.Cancel();

            foreach (Task? task in new[] { _receiveTask, _pingTask })
            {
                if (task is null)
                    continue;

                try { await task; }
                catch (Exception) { }
            }

            _receiveTask = null;
            _pingTask = null;

            if (IsConnected)
            {
                try { await _webSocket!.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None); }
                catch (Exception) { }
            }

            _cancellation?.Dispose();
            _webSocket?.Dispose();
            _sendLock?.Dispose();

            _cancellation = null;
            _webSocket = null;
            _sendLock = null;
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectFromUserhub();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Data processing

        private async Task ReceiveLoop(CancellationToken token)
        {
            const string LOG_IDENT = "RealtimeMessaging::ReceiveLoop";

            try
            {
                while (!token.IsCancellationRequested && IsConnected)
                {
                    string frame;

                    using (var stream = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        byte[] buffer = new byte[4096];

                        do
                        {
                            result = await _webSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                            if (result.MessageType == WebSocketMessageType.Close)
                                return;

                            await stream.WriteAsync(buffer, 0, result.Count, token);
                        }
                        while (!result.EndOfMessage);

                        frame = Encoding.UTF8.GetString(stream.ToArray());
                    }

                    foreach (string record in frame.Split(RecordSeparator, StringSplitOptions.RemoveEmptyEntries))
                        HandleRecord(record.TrimEnd('\0'), LOG_IDENT);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Connection dropped");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void HandleRecord(string record, string logIdent)
        {
            if (String.IsNullOrWhiteSpace(record))
                return;

            try
            {
                var message = JsonSerializer.Deserialize<SignalrMessage>(record);

                if (message is null || message.Type is null || message.Type == PingType)
                    return;

                if (message.Target == "subscriptionStatus")
                    return;

                if (message.Target is null || message.Arguments is null || message.Arguments.Length < 2)
                    return;

                ProcessEvent(message);
            }
            catch (JsonException ex)
            {
                App.Logger.WriteLine(logIdent, $"Failed to deserialize record\nRaw record:\n{record}");
                App.Logger.WriteException(logIdent, ex);
            }
        }

        private async Task PingLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsConnected)
                {
                    await SendPing();
                    await Task.Delay(PingIntervalMs, token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task SendHandshake() => await SafeSend("{\"protocol\":\"json\",\"version\":1}");

        private async Task SendPing() => await SafeSend($"{{\"type\":{PingType}}}");

        private async Task SafeSend(string payload)
        {
            const string LOG_IDENT = "RealtimeMessaging::SafeSend";

            if (!IsConnected || _sendLock is null)
            {
                App.Logger.WriteLine(LOG_IDENT, "Tried to send after the socket closed");
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(payload + RecordSeparator);

            try
            {
                await _sendLock.WaitAsync();
                await _webSocket!.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to send");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                _sendLock?.Release();
            }
        }

        private void ProcessEvent(SignalrMessage e)
        {
            const string LOG_IDENT = "RealtimeMessaging::ProcessEvent";

            string target = e.Arguments![0];
            string payload = e.Arguments![1];

            try
            {
                switch (target)
                {
                    case "CommunicationChannels":
                        var message = JsonSerializer.Deserialize<MessageEvent>(payload);

                        if (message is null)
                            throw new JsonException("Deserialised MessageEvent is null");

                        PartyChat?.Invoke(this, message);
                        break;

                    default:
                        App.Logger.WriteLine(LOG_IDENT, $"Unhandled message target: {target}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to deserialize a '{target}' payload");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        #endregion
    }
}
