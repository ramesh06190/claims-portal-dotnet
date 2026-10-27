using Microsoft.AspNetCore.Mvc;

namespace ClaimsPortal.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(model: HttpContext.TraceIdentifier);
}
