using System.Security.Cryptography;
using System.Windows;

using Bloxstrap.UI.Elements.Dialogs;

using DiscordRPC;
using DiscordRPC.Message;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloxstrap.Integrations
{
    public static class DiscordInvites
    {
        private class DesktopJoinConverter : Newtonsoft.Json.JsonConverter<DiscordRPC.RichPresence>
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, DiscordRPC.RichPresence? value, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (value is null)
                {
                    writer.WriteNull();
                    return;
                }

                JObject activity = JObject.FromObject(value, Newtonsoft.Json.JsonSerializer.Create());

                activity["name"] = DiscordRichPresence.ActivityName;

                if (activity["secrets"] is JObject secrets)
                    secrets.Remove("MatchSecret");

                if (activity["party"] is JObject party)
                    party.Remove("size");

                if (value.HasSecrets())
                    activity["supported_platforms"] = new JArray("desktop");

                activity.WriteTo(writer);
            }

            public override DiscordRPC.RichPresence? ReadJson(JsonReader reader, Type objectType, DiscordRPC.RichPresence? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer) =>
                throw new NotSupportedException();
        }

        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(5);

        private static readonly Regex SecretPattern = new(@"^([0-9]{1,20}):([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$", RegexOptions.IgnoreCase);

        private static string JoinCommand => $"\"{Paths.Process}\" -discordjoin";

        public static bool CanHost(ActivityData activity) =>
            App.Settings.Prop.AllowDiscordInvites
            && activity.ServerType == ServerType.Public
            && !String.IsNullOrEmpty(activity.JobId);

        public static Party CreateParty(ActivityData activity) => new()
        {
            ID = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(activity.JobId)))[..32],
            Privacy = Party.PrivacySetting.Private
        };

        public static Secrets CreateSecrets(ActivityData activity) => new()
        {
            JoinSecret = $"{activity.PlaceId}:{activity.JobId}"
        };

        public static bool Register(DiscordRpcClient client)
        {
            const string LOG_IDENT = "DiscordInvites::Register";

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Converters = { new DesktopJoinConverter() } };

            try
            {
                return client.RegisterUriScheme(null, JoinCommand);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to register the Discord join command");
                App.Logger.WriteException(LOG_IDENT, ex);
                return false;
            }
        }

        public static bool Join(string? secret)
        {
            const string LOG_IDENT = "DiscordInvites::Join";

            Match match = SecretPattern.Match(secret ?? "");

            if (!match.Success)
            {
                App.Logger.WriteLine(LOG_IDENT, "Received an invalid join secret");
                return false;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Joining {match.Groups[1].Value}/{match.Groups[2].Value} from a Discord invite");

            Process.Start(Paths.Process, $"roblox://experiences/start?placeId={match.Groups[1].Value}&gameInstanceId={match.Groups[2].Value}");

            return true;
        }

        public static bool AskToJoin(JoinRequestMessage request)
        {
            string name = String.IsNullOrEmpty(request.User.DisplayName) ? request.User.Username : request.User.DisplayName;
            string message = String.Format(Strings.Dialog_DiscordInvites_JoinRequest, name);

            return Application.Current.Dispatcher.Invoke(() =>
            {
                var messagebox = new FluentMessageBox(message, MessageBoxImage.Question, MessageBoxButton.YesNo);
                messagebox.ShowDialog();
                return messagebox.Result == MessageBoxResult.Yes;
            });
        }

        public static async Task<string?> WaitForJoinSecret()
        {
            const string LOG_IDENT = "DiscordInvites::WaitForJoinSecret";

            var received = new TaskCompletionSource<string>();
            var subscribed = new TaskCompletionSource();

            using var client = new DiscordRpcClient(DiscordRichPresence.ApplicationId);

            client.OnJoin += (_, e) => received.TrySetResult(e.Secret);
            client.OnSubscribe += (_, e) =>
            {
                if (e.Event == EventType.Join)
                    subscribed.TrySetResult();
            };
            client.OnError += (_, e) => App.Logger.WriteLine(LOG_IDENT, $"An RPC error occurred - {e.Message}");

            if (!Register(client))
                return null;

            client.Initialize();
            client.Subscribe(EventType.Join);

            App.Logger.WriteLine(LOG_IDENT, "Waiting for Discord to send the invite");

            await Task.WhenAny(received.Task, subscribed.Task, Task.Delay(ConnectTimeout));

            if (!received.Task.IsCompleted && !subscribed.Task.IsCompleted)
            {
                App.Logger.WriteLine(LOG_IDENT, "Timed out connecting to Discord");
                return null;
            }

            if (await Task.WhenAny(received.Task, Task.Delay(JoinTimeout)) != received.Task)
            {
                App.Logger.WriteLine(LOG_IDENT, "Discord didn't send an invite");
                return null;
            }

            return received.Task.Result;
        }
    }
}
