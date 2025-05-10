using System.ComponentModel.DataAnnotations;

namespace ABPNET.Models.TokenAuth
{
    public class EmailVerificationModel
    {
        [Required]
        public long UserId { get; set; }
    }
}



