using System.ComponentModel.DataAnnotations;
using Abp.Authorization.Users;

namespace ABPNET.Models.TokenAuth
{
    public class ExternalAuthenticateSocialModel
    {
        [Required]
        [StringLength(UserLogin.MaxLoginProviderLength)]
        public string AuthProvider { get; set; }

        [Required]
        [StringLength(UserLogin.MaxProviderKeyLength)]
        public string ProviderKey { get; set; }

        [Required]
        public string DisplayName { get; set; }

        [Required]
        public string AccessToken { get; set; }

        [Required]
        public string Name { get; set; }

        public string SurName { get; set; }

        [Required]
        public string EmailAddress { get; set; }
    }
}



