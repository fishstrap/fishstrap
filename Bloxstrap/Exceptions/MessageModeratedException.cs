namespace Bloxstrap.Exceptions
{
    internal class MessageModeratedException : Exception
    {
        public MessageModeratedException()
            : base("Message was moderated by the platform.")
        {
        }
    }
}
