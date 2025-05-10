namespace ABPNET.Configuration
{
    public class MailConfiguration
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool SSL { get; set; }

        public string EmailFrom { get; set; }

        public int EmailTimeout { get; set; }

        public string SenderName { get; set; }
    }

    public interface IEmailGetter
    {
        void GetEmailBody(EmailType email);
    }

    public enum EmailType
    {
        VerifyAccountEmail
    }
}



