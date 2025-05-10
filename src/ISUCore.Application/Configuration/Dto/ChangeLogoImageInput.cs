using System.ComponentModel.DataAnnotations;

namespace ISUCore.Configuration.Dto
{
    public class ChangeLogoImageInput
    {
        [Required]
        public bool Allowed { get; set; }
    }
}

