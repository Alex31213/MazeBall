using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MazeBall.Controllers
{
    [Route("game"), Authorize]
    [Produces("application/json")]
    public class GameController : Controller
    {

    }
}
