using Microsoft.AspNetCore.Mvc;

namespace Vista1Danielote.Controllers;

public class ModulosController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (HttpContext.Session.GetInt32("Rol") != 1)
            return RedirectToAction("Login", "Account");
        return View();
    }
}
