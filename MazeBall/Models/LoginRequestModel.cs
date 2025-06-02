using System.ComponentModel.DataAnnotations;

namespace MazeBall.Models
{
    public class LoginRequestModel
    {
        [Required(ErrorMessage = "Username required!")]
        [MaxLength(50, ErrorMessage = "Username too long!")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password required!")]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters long!")]
        public string Password { get; set; }
    }
}
