using MazeBall.Database.CRUDs;
using MazeBall.Database.Entities;
using MazeBall.Dto;
using MazeBall.Models;
using MazeBall.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Globalization;

namespace MazeBall.Controllers
{
    [Produces("application/json")]
    public class AuthentificationController : ControllerBase
    {
        private MazeBallContext dbContext;
        private IUserAuthorizationService _authorization;
        private static ConcurrentBag<string> loggedUsers = new ConcurrentBag<string>();

        public AuthentificationController(IUserAuthorizationService authorization, MazeBallContext dbContext)
        {
            _authorization = authorization;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Verify introduced credentials in order to login User
        /// </summary>
        /// <remarks>
        /// Get a jwt token
        /// </remarks>
        /// <response code="200">User</response> 
        [HttpPost, AllowAnonymous]
        [Route("login")]
        public ActionResult Login([FromBody] LoginRequestModel user) => LoginUser(user);

        /// <summary>
        /// Logout user
        /// </summary>
        /// <remarks>
        /// Get a jwt token
        /// </remarks>
        /// <response code="200">User</response> 
        [HttpPost]
        [Route("logout")]
        public ActionResult Logout() => LogoutUser();

        /// <summary>
        /// Add new user to DB and verifying for username to be unique
        /// </summary>
        /// <response code="200">User</response>
        [HttpPost, AllowAnonymous]
        [Route("register")]
        public ActionResult Register([FromBody] RegisterRequestModel user) => RegisterUser(user);

        /// <summary>
        /// Check token
        /// </summary>
        /// <remarks>
        /// Get a jwt token
        /// </remarks>
        /// <response code="200">User</response> 
        [HttpPost, AllowAnonymous]
        [Route("checkToken")]
        public ActionResult CheckToken([FromBody] CheckTokenRequestModel user) => CheckUserToken(user);

        /// <summary>
        /// Get all user from DB
        /// </summary>
        /// /// <remarks>
        /// Returns empty enumerable object if there are no users in DB
        /// </remarks>
        [HttpPost, Authorize]
        [Route("uploadProfileImage")]
        [Produces("text/plain")]
        public ActionResult UploadProfileImage([FromServices] IConfiguration configuration)
            => UploadUserProfileImage(configuration);

        [HttpPost, Authorize]
        [Route("getUserProfileImage")]
        public ActionResult GetProfileImage([FromBody] ProfilePictureRequestModel model,
            [FromServices] IConfiguration configuration)
            => GetUserProfileImage(model, configuration);

        private ActionResult LoginUser(LoginRequestModel user)
        {
            UserCRUD userCRUD = new UserCRUD(dbContext);
            var foundUser = userCRUD.GetUserByUsername(user.Username);
            if (foundUser == null) return BadRequest("User not found!");

            var samePassword = _authorization.VerifyHashedPassword(foundUser.Password, user.Password);
            if (!samePassword) return BadRequest("Invalid password!");

            bool loggedIn = loggedUsers.Any(u => u == user.Username);

            if (loggedIn) return BadRequest("Already logged in");

            var user_jsonWebToken = _authorization.GetToken(foundUser);
            loggedUsers.Add(user.Username);

            return Ok(new ResponseLogin
            {
                Token = user_jsonWebToken
            });
        }

        private ActionResult LogoutUser()
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            if (username == null) return BadRequest("User not found! Failed logout");

            ConcurrentBag<string> updatedBag = new ConcurrentBag<string>();
            bool found = false;

            while (loggedUsers.TryTake(out string item))
            {
                if (item != username)
                {
                    updatedBag.Add(item);
                }
                else
                {
                    found = true;
                }
            }

            if (!found) return BadRequest("User not found in the logged-in users list! Failed logout.");

            loggedUsers = updatedBag;

            return Ok();
        }

        public ActionResult UploadUserProfileImage([FromServices] IConfiguration configuration)
        {
            var profileImage = Request.Form.Files["profileImage"];

            if (profileImage != null && profileImage.Length > 0)
            {
                var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
                if (username == null) return BadRequest("User claims not found!");
                var fileExtension = Path.GetExtension(profileImage.FileName);
                var currentDirectory = Directory.GetCurrentDirectory();
                var userProfileImagesPath = Path.Combine(currentDirectory, "UserProfileImages");
                if (!Directory.Exists(userProfileImagesPath))
                {
                    Directory.CreateDirectory(userProfileImagesPath);
                }
                var fileName = $"{username}{fileExtension}";
                var imagePath = Path.Combine(userProfileImagesPath, fileName);
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    profileImage.CopyTo(stream);
                }
                return Ok();
            }
            return BadRequest("No image provided.");
        }

        public ActionResult GetUserProfileImage([FromBody] ProfilePictureRequestModel model, [FromServices] IConfiguration configuration)
        {
            string username = model.Username;
            var currentDirectory = Directory.GetCurrentDirectory();
            var userProfileImagesPath = Path.Combine(currentDirectory, "UserProfileImages");
            var imagePath = Path.Combine(userProfileImagesPath, $"{username}.jpg");
            if (!System.IO.File.Exists(imagePath))
            {
                Console.WriteLine($"User '{username}' Profile Image not found. " +
                    "Sending default Profile Image.");
                imagePath = Path.Combine(currentDirectory, "DefaultPhotos", "DefaultProfileImage.jpg");
            }
            var imageBytes = System.IO.File.ReadAllBytes(imagePath);
            return File(imageBytes, "image/jpeg");
        }

        private ActionResult RegisterUser(RegisterRequestModel user)
        {
            UserCRUD userCRUD = new UserCRUD(dbContext);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            else
            {
                if (userCRUD.GetUserByUsername(user.Username) != null)
                {
                    return BadRequest("Username already exists!");
                }

                var userToAdd = new User
                {
                    Username = user.Username,
                    Password = _authorization.HashPassword(user.Password),
                    Email = user.Email,
                    BirthDate = DateTime.ParseExact(user.BirthDate, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                };
                userCRUD.Add(userToAdd);
                return Ok(new { message = $"Registered successfully user = {user.Username}" });
            }
        }

        private ActionResult CheckUserToken(CheckTokenRequestModel token)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            else
            {
                if (_authorization.ValidateToken(token.Token))
                {
                    return Ok("true");
                }
                else
                {
                    return BadRequest("Token invalid!");
                }
            }
        }
    }
}
