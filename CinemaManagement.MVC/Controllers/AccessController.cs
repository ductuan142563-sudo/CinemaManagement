using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

public class AccessController : Controller
{
    [HttpGet]
    public IActionResult Denied()
    {
        Response.StatusCode = 403;
        return View();
    }
}