using Microsoft.AspNetCore.Mvc;

namespace Vistasdebeltran.Controllers;

public class PerfilCuentaController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Perfil", new { seccion = "membresia" });
    }
}
