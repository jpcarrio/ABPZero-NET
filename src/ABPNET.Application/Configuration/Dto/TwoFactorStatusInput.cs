using System.ComponentModel.DataAnnotations;

namespace ABPNET.Configuration.Dto
{
    public class TwoFactorStatusInput
    {
        [Required]
        public bool Active { get; set; }
    }
}



