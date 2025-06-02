using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MazeBall.Controllers
{
    [Route("lobby"), Authorize]
    [Produces("application/json")]
    public class LobbyController : Controller
    {

    }
}
