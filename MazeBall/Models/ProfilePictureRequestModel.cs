using System.ComponentModel.DataAnnotations;

namespace MazeBall.Models
{
    public class ProfilePictureRequestModel
    {
        [Required(ErrorMessage = "Username required!")]
        [MaxLength(50, ErrorMessage = "Username too long!")]
        public string Username { get; set; }
    }
}
