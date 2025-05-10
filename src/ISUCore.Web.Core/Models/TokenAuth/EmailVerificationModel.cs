using System.ComponentModel.DataAnnotations;

namespace ISUCore.Models.TokenAuth
{
    public class EmailVerificationModel
    {
        [Required]
        public long UserId { get; set; }
    }
}

