using System.ComponentModel.DataAnnotations;

namespace ISUCore.Configuration.Dto
{
    public class GlobalSettingsInput
    {
        [Required]
        [StringLength(32)]
        public string AppName { get; set; }
    }
}

