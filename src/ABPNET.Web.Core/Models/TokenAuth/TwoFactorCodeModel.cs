using System.ComponentModel.DataAnnotations;
namespace ABPNET.Models.TokenAuth
{
    public class TwoFactorCodeModel
    {
        [Required]
        public string Code { get; set; }
    }
}



