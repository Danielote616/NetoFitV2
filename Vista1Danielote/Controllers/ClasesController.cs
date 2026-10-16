
using Microsoft.AspNetCore.Mvc;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class ClasesController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? tipo, string? sucursal)
        {
            ClasesDAL datos = new ClasesDAL();

            List<ClaseHorarioModelo> todas = datos.ObtenerClases();

            List<ClaseHorarioModelo> clases =
                new List<ClaseHorarioModelo>();

            List<string> tipos = new List<string>();

            List<string> sucursales = new List<string>();

            foreach (ClaseHorarioModelo clase in todas)
            {
                if (!tipos.Contains(clase.Nombre))
                {
                    tipos.Add(clase.Nombre);
                }

                if (!string.IsNullOrWhiteSpace(clase.Sucursal))
                {
                    if (!sucursales.Contains(clase.Sucursal))
                    {
                        sucursales.Add(clase.Sucursal);
                    }
                }

                bool coincideTipo = false;
                bool coincideSucursal = false;

                if (string.IsNullOrWhiteSpace(tipo) ||
                    clase.Nombre == tipo)
                {
                    coincideTipo = true;
                }

                if (string.IsNullOrWhiteSpace(sucursal) ||
                    clase.Sucursal == sucursal)
                {
                    coincideSucursal = true;
                }

                if (coincideTipo && coincideSucursal)
                {
                    clases.Add(clase);
                }
            }

            tipos.Sort();
            sucursales.Sort();

            ViewBag.TiposClases = tipos;
            ViewBag.Sucursales = sucursales;
            ViewBag.SucursalSeleccionada = sucursal;
            ViewBag.TipoSeleccionado = tipo;

            return View(clases);
        }
    }
}
