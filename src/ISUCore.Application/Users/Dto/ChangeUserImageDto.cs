using System.ComponentModel.DataAnnotations;

namespace ISUCore.Users.Dto
{
    public class ChangeUserImageDto
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public string UserImage { get; set; }

        [Required]
        public string UserImageFileName { get; set; }
    }
}

