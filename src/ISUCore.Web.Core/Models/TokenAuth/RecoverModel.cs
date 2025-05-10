using System.ComponentModel.DataAnnotations;
using Abp.Authorization.Users;

namespace ISUCore.Models.TokenAuth
{
    public class RecoverModel
    {
        [Required]
        [StringLength(AbpUserBase.MaxEmailAddressLength)]
        public string EmailAddress { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public int UserId { get; set; }

        public string Token { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}

