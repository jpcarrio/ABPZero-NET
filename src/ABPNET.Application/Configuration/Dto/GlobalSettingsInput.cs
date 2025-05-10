using System.ComponentModel.DataAnnotations;

namespace ABPNET.Configuration.Dto
{
    public class GlobalSettingsInput
    {
        [Required]
        [StringLength(32)]
        public string AppName { get; set; }
    }
}



