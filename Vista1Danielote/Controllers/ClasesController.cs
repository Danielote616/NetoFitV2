using Microsoft.AspNetCore.Mvc;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers;

public class ClasesController : Controller
{
    [HttpGet]
    public IActionResult Index(string? tipo, string? sucursal)
    {
        ClasesDAL datos = new ClasesDAL();
        List<ClaseHorarioModelo> todas = datos.ObtenerClases();
        List<ClaseHorarioModelo> clases = new List<ClaseHorarioModelo>();
        List<string> tipos = new List<string>();

        List<string> sucursales = todas.Select(x => x.Sucursal).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();

        foreach (ClaseHorarioModelo clase in todas)
        {
            if (!tipos.Contains(clase.Nombre))
            {
                tipos.Add(clase.Nombre);
            }
            if ((string.IsNullOrWhiteSpace(tipo) || clase.Nombre == tipo) && (string.IsNullOrWhiteSpace(sucursal) || clase.Sucursal == sucursal))
            {
                clases.Add(clase);
            }
        }

        ViewBag.TiposClases = tipos;
        ViewBag.Sucursales = sucursales;
        ViewBag.SucursalSeleccionada = sucursal;
        ViewBag.TipoSeleccionado = tipo;
        return View(clases);
    }
}
