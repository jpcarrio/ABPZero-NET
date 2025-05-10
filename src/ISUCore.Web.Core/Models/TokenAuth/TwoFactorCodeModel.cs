using System.ComponentModel.DataAnnotations;
namespace ISUCore.Models.TokenAuth
{
    public class TwoFactorCodeModel
    {
        [Required]
        public string Code { get; set; }
    }
}

