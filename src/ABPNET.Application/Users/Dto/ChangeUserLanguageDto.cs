using System.ComponentModel.DataAnnotations;

namespace ABPNET.Users.Dto
{
    public class ChangeUserLanguageDto
    {
        [Required]
        public string LanguageName { get; set; }
    }
}


