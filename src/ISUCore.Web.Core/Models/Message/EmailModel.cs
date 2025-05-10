using System.ComponentModel.DataAnnotations;
namespace ISUCore.Models.Message
{
    public class EmailModel
    {
        public EmailModel()
        {
            EmailBodyLines = System.Array.Empty<string>();
        }
        public string AppLogo { get; set; }
        public string AppName { get; set; }
        public string Html { get; set; }
        public string Subject { get; set; }
        public string ToAddress { get; set; }
        public string ToDisplayName { get; set; }
        [Required]
        public string ServerDomain { get; set; }
        public string RedirectUrl { get; set; }
        public string UserName { get; set; }
        public string EmailTitle { get; set; }
        public string EmailIntroText { get; set; }
        public string[] EmailBodyLines { get; set; }
        public string EmailButtonText { get; set; }
    }
}

