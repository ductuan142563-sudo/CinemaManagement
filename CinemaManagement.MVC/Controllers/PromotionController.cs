using CinemaManagement.MVC.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

public class PromotionController : Controller
{
    // GET: /Promotion
    [HttpGet]
    public IActionResult Index()
    {
        return View(PromotionCatalog.All);
    }
}