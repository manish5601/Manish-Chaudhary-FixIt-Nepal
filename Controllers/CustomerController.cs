using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixItNepal.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
