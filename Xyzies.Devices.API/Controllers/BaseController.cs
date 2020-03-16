using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Xyzies.Devices.API.Controllers
{
    /// <summary>
    /// Base controller
    /// </summary>
    public class BaseController : Controller
    {
        /// <summary>
        /// Authorize token
        /// </summary>
        public string Token
        {
            get => HttpContext.Request.Headers["Authorization"].ToString().Split(' ').LastOrDefault();
        }
    }
}
