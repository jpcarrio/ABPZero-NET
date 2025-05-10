using System.ComponentModel.DataAnnotations;

namespace ISUCore.Models.TokenAuth
{
    public class MailContentModel
    {
        [Required]
        public string RedirectUrl { get; set; }
        public string UserName { get; set; }
        public string ServerDomain { get; set; }
    }
}


