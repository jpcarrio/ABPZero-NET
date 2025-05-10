using System.ComponentModel.DataAnnotations;

namespace ABPNET.Configuration.Dto
{
    public class ChangeLogoImageInput
    {
        [Required]
        public bool Allowed { get; set; }
    }
}



