using Bloxstrap.Models.APIs.RobloxParty;
using Bloxstrap.Models.APIs.RobloxParty.Events;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.Integrations.OverlayModules
{
    public class RobloxParty
    {
        private const string ApiService = "apis";
        private const string ApiPath = "platform-chat-api/v1";

        private readonly RealtimeMessaging? _messaging;

        public event EventHandler<MessageEvent>? IncomingMessage;

        public RobloxParty(RealtimeMessaging? messaging)
        {
            if (messaging is null)
                return;

            _messaging = messaging;
            _messaging.PartyChat += OnIncomingMessage;
        }

        private void OnIncomingMessage(object? sender, MessageEvent message) =>
            IncomingMessage?.Invoke(this, message);

        public async Task<ConversationsPage?> GetConversations(int pageSize = 20, string? cursor = null)
        {
            const string LOG_IDENT = "RobloxParty::GetConversations";

            Uri url = UrlBuilder.BuildApiUrl(ApiService,
                $"{ApiPath}/get-user-conversations?pageSize={pageSize}&include_user_data=true&cursor={cursor}");

            try
            {
                return await Http.AuthGetJson<ConversationsPage>(url);
            }
            catch (Exception ex) when (ex is JsonException || ex is HttpRequestException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get conversations");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public async Task<List<Conversation>> GetAllConversations(int maxPages = 5)
        {
            var conversations = new List<Conversation>();
            string? cursor = null;

            for (int page = 0; page < maxPages; page++)
            {
                ConversationsPage? result = await GetConversations(50, cursor);

                if (result is null)
                    break;

                conversations.AddRange(result.Conversations);

                cursor = result.NextCursor;

                if (String.IsNullOrEmpty(cursor))
                    break;
            }

            return conversations;
        }

        public async Task<Conversation?> CreateConversation(long userId)
        {
            const string LOG_IDENT = "RobloxParty::CreateConversation";

            var payload = new
            {
                conversations = new[] { new { type = "one_to_one", participant_user_ids = new[] { userId } } },
                include_user_data = false
            };

            try
            {
                var result = await AccountRequests.PostJsonAsync<ConversationsPage>(
                    UrlBuilder.BuildApiUrl(ApiService, $"{ApiPath}/create-conversations"), payload);

                return result?.Conversations.FirstOrDefault(x => !String.IsNullOrEmpty(x.Id));
            }
            catch (Exception ex) when (ex is JsonException || ex is HttpRequestException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to start a conversation");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public async Task<UserMessagesPage?> GetMessages(Conversation conversation, string? cursor = null)
        {
            const string LOG_IDENT = "RobloxParty::GetMessages";

            if (String.IsNullOrEmpty(conversation.Id))
                return null;

            Uri url = UrlBuilder.BuildApiUrl(ApiService,
                $"{ApiPath}/get-conversation-messages?conversation_id={conversation.Id}&cursor={cursor}");

            try
            {
                return await Http.AuthGetJson<UserMessagesPage>(url);
            }
            catch (Exception ex) when (ex is JsonException || ex is HttpRequestException)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get messages for {conversation.Id}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public async Task<UserMessage?> SendMessage(string conversationId, string messageContent)
        {
            const string LOG_IDENT = "RobloxParty::SendMessage";

            var payload = new MessagesContents
            {
                ConversationId = conversationId,
                Messages = new[] { new MessageContent { Content = messageContent } }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            string csrf = await App.Cookies.GetXCSRF();

            using var response = await App.Cookies.AuthPost(
                UrlBuilder.BuildApiUrl(ApiService, $"{ApiPath}/send-messages"), content, csrf);

            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<UserMessagesPage>(await response.Content.ReadAsStringAsync());

            if (result is null)
                return null;

            foreach (UserMessage message in result.Messages)
            {
                if (message.Status != "moderated")
                    continue;

                App.Logger.WriteLine(LOG_IDENT, "Message was moderated");

                throw new InvalidOperationException("Message was moderated by the platform.");
            }

            return result.Messages.FirstOrDefault();
        }

        public async Task UpdateTypingStatus(Conversation conversation)
        {
            const string LOG_IDENT = "RobloxParty::UpdateTypingStatus";

            var payload = new ConversationPayload { Id = conversation.Id };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                string csrf = await App.Cookies.GetXCSRF();

                using var response = await App.Cookies.AuthPost(
                    UrlBuilder.BuildApiUrl(ApiService, $"{ApiPath}/update-typing-status"), content, csrf);
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to update typing status");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
