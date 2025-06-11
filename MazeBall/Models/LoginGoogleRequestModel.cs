using System.ComponentModel.DataAnnotations;

namespace MazeBall.Models
{
    public class LoginGoogleRequestModel
    {
        [Required(ErrorMessage = "Email required!")]
        [MinLength(5, ErrorMessage = "Email must be at least 5 characters long!")]
        public string Email { get; set; }
    }
}
