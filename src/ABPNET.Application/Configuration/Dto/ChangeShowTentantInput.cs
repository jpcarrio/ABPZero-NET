using System.ComponentModel.DataAnnotations;

namespace ABPNET.Configuration.Dto
{
    public class ChangeShowTentantInput
    {
        [Required]
        public bool Show { get; set; }
    }
}



