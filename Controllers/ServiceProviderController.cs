using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixItNepal.Controllers
{
    [Authorize(Roles = "ServiceProvider")]
    public class ServiceProviderController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
