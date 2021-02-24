namespace Peep.Core.API.Options
{
    public class MessagingOptions
    {
        public const string Key = "Messaging";

        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
