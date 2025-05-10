using System.ComponentModel.DataAnnotations;

namespace ISUCore.Configuration.Dto
{
    public class ChangeShowTentantInput
    {
        [Required]
        public bool Show { get; set; }
    }
}

