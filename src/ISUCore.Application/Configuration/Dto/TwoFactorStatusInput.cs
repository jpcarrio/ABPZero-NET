using System.ComponentModel.DataAnnotations;

namespace ISUCore.Configuration.Dto
{
    public class TwoFactorStatusInput
    {
        [Required]
        public bool Active { get; set; }
    }
}

