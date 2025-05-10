using System;
namespace ISUCore.Models.TokenAuth
{
    [Serializable]
    public class EmailSendingArgs
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Sender { get; set; }
        public string From { get; set; }
    }
}

