using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers;

public class ClientesController : Controller
{
    [HttpGet]
    public IActionResult Index(string? busqueda, string estado = "Todos")
    {
        if (HttpContext.Session.GetInt32("Rol") != 1)
            return RedirectToAction("Login", "Account");
        ClientesDAL datos = new ClientesDAL();
        List<ClienteModelo> todos = datos.ObtenerClientes();
        List<ClienteModelo> clientes = new List<ClienteModelo>();
        int totalClientes = todos.Count;

        busqueda = busqueda?.Trim() ?? "";
        if (!string.Equals(estado, "Activa", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(estado, "Por vencer", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(estado, "Vencida", StringComparison.OrdinalIgnoreCase))
        {
            estado = "Todos";
        }
        else if (string.Equals(estado, "Activa", StringComparison.OrdinalIgnoreCase))
        {
            estado = "Activa";
        }
        else if (string.Equals(estado, "Por vencer", StringComparison.OrdinalIgnoreCase))
        {
            estado = "Por vencer";
        }
        else
        {
            estado = "Vencida";
        }

        foreach (ClienteModelo cliente in todos)
        {
            bool coincideBusqueda = busqueda == ""
                || cliente.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                || cliente.Correo.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                || cliente.Telefono.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                || cliente.Plan.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                || cliente.Sucursal.Contains(busqueda, StringComparison.OrdinalIgnoreCase);
            bool coincideEstado = estado == "Todos" || cliente.Estado == estado;
            if (coincideBusqueda && coincideEstado)
            {
                clientes.Add(cliente);
            }
        }
        ViewBag.Busqueda = busqueda;
        ViewBag.Estado = estado;
        ViewBag.TotalClientes = totalClientes;
        return View(clientes);
    }

    [HttpGet]
    public IActionResult Detalle(int id)
    {
        if (HttpContext.Session.GetInt32("Rol") != 1)
            return RedirectToAction("Login", "Account");
        ClientesDAL datos = new ClientesDAL();
        ClienteModelo? cliente = datos.ObtenerPorId(id);
        if (cliente == null) { return NotFound(); }
        return View(cliente);
    }


    [HttpGet]
    public IActionResult Formulario(int? id)
    {
        if (HttpContext.Session.GetInt32("Rol") != 1) return RedirectToAction("Login", "Account");
        var modelo = new ClienteFormularioAdminModelo();
        if (id.HasValue)
        {
            using (SqlConnection con = new Conexion().GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id_cliente, nombre, email, telefono, fecha_nacimiento FROM Cliente WHERE id_cliente=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return NotFound();
                        modelo.Id = reader.GetInt32(0); modelo.Nombre = reader.GetString(1);
                        modelo.Correo = reader.GetString(2); modelo.Telefono = reader[3]?.ToString() ?? "";
                        modelo.FechaNacimiento = reader[4] == DBNull.Value ? null : Convert.ToDateTime(reader[4]);
                    }
                }
            }
        }
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Formulario(ClienteFormularioAdminModelo modelo)
    {
        if (HttpContext.Session.GetInt32("Rol") != 1) return RedirectToAction("Login", "Account");
        string correo = modelo.Correo?.Trim().ToLowerInvariant() ?? "";
        if (modelo.Id == 0 && (string.IsNullOrWhiteSpace(modelo.Contrasena) || modelo.Contrasena.Length < 8 || !modelo.Contrasena.Any(char.IsLetter) || !modelo.Contrasena.Any(char.IsDigit)))
            ModelState.AddModelError(nameof(modelo.Contrasena), "La contraseña de prueba debe tener ocho caracteres, letras y números");
        if (modelo.FechaNacimiento.HasValue && modelo.FechaNacimiento.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(modelo.FechaNacimiento), "No se permiten fechas futuras");
        if (!ModelState.IsValid) return View(modelo);
        using (SqlConnection con = new Conexion().GetConnection())
        {
            con.Open();
            using (SqlCommand cmd = new SqlCommand(@"SELECT COUNT(*) FROM Cliente WHERE email=@Email AND id_cliente<>@Id
                UNION ALL SELECT COUNT(*) FROM Administrador WHERE email=@Email", con))
            {
                cmd.Parameters.AddWithValue("@Email", correo); cmd.Parameters.AddWithValue("@Id", modelo.Id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int cantidad = 0;
                    while (reader.Read()) cantidad += Convert.ToInt32(reader[0]);
                    if (cantidad > 0) ModelState.AddModelError(nameof(modelo.Correo), "Este correo ya está registrado");
                }
            }
            if (!ModelState.IsValid) return View(modelo);
            string sql = modelo.Id == 0
                ? "INSERT INTO Cliente (nombre, email, [contraseña], telefono, fecha_nacimiento) VALUES (@Nombre, @Email, @Clave, @Telefono, @Fecha)"
                : string.IsNullOrEmpty(modelo.Contrasena)
                ? "UPDATE Cliente SET nombre=@Nombre, email=@Email, telefono=@Telefono, fecha_nacimiento=@Fecha WHERE id_cliente=@Id"
                : "UPDATE Cliente SET nombre=@Nombre, email=@Email, telefono=@Telefono, fecha_nacimiento=@Fecha, [contraseña]=@Clave WHERE id_cliente=@Id";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Id", modelo.Id); cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre.Trim());
                cmd.Parameters.AddWithValue("@Email", correo); cmd.Parameters.AddWithValue("@Clave", modelo.Contrasena);
                cmd.Parameters.AddWithValue("@Telefono", modelo.Telefono.Trim());
                cmd.Parameters.AddWithValue("@Fecha", modelo.FechaNacimiento!.Value.Date);
                cmd.ExecuteNonQuery();
            }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        if (HttpContext.Session.GetInt32("Rol") != 1) return RedirectToAction("Login", "Account");
        try
        {
            using (SqlConnection con = new Conexion().GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM Cliente WHERE id_cliente=@Id", con))
                { cmd.Parameters.AddWithValue("@Id", id); cmd.ExecuteNonQuery(); }
            }
        }
        catch (SqlException) { TempData["Error"] = "El cliente tiene membresías, reservas u otros registros asociados y no se puede eliminar."; }
        return RedirectToAction(nameof(Index));
    }
}
