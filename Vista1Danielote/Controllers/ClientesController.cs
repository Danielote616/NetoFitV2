
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class ClientesController : Controller
    {
        [HttpGet]
        public IActionResult Index(
            string? busqueda,
            string estado = "Todos")
        {
            if (HttpContext.Session.GetInt32("Rol") != 1)
            {
                return RedirectToAction("Login", "Account");
            }

            ClientesDAL datos = new ClientesDAL();

            List<ClienteModelo> todos = datos.ObtenerClientes();
            List<ClienteModelo> clientes = new List<ClienteModelo>();

            int totalClientes = todos.Count;

            busqueda = busqueda?.Trim() ?? "";

            if (string.Equals(estado, "Activa",
                StringComparison.OrdinalIgnoreCase))
            {
                estado = "Activa";
            }
            else if (string.Equals(estado, "Por vencer",
                StringComparison.OrdinalIgnoreCase))
            {
                estado = "Por vencer";
            }
            else if (string.Equals(estado, "Vencida",
                StringComparison.OrdinalIgnoreCase))
            {
                estado = "Vencida";
            }
            else if (string.Equals(estado, "Pendiente",
                StringComparison.OrdinalIgnoreCase))
            {
                estado = "Pendiente";
            }
            else if (string.Equals(estado, "Programada",
                StringComparison.OrdinalIgnoreCase))
            {
                estado = "Programada";
            }
            else
            {
                estado = "Todos";
            }

            foreach (ClienteModelo cliente in todos)
            {
                bool coincideBusqueda =
                    busqueda == "" ||
                    cliente.Nombre.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase) ||
                    cliente.Correo.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase) ||
                    cliente.Telefono.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase) ||
                    cliente.Plan.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase) ||
                    cliente.Sucursal.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase);

                bool coincideEstado =
                    estado == "Todos" || cliente.Estado == estado;

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
            {
                return RedirectToAction("Login", "Account");
            }

            ClientesDAL datos = new ClientesDAL();
            ClienteModelo? cliente = datos.ObtenerPorId(id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        [HttpGet]
        public IActionResult Formulario(int? id)
        {
            if (HttpContext.Session.GetInt32("Rol") != 1)
            {
                return RedirectToAction("Login", "Account");
            }

            ClienteFormularioAdminModelo modelo =
                new ClienteFormularioAdminModelo();

            if (id.HasValue)
            {
                using (SqlConnection con =
                    new Conexion().GetConnection())
                {
                    con.Open();

                    string sql = @"SELECT id_cliente, nombre, email,
                                          telefono, fecha_nacimiento,
                                          ruta_imagen
                                   FROM Cliente
                                   WHERE id_cliente = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return NotFound();
                            }

                            modelo.Id = reader.GetInt32(0);
                            modelo.Nombre = reader.GetString(1);
                            modelo.Correo = reader.GetString(2);

                            modelo.Telefono = reader[3] == DBNull.Value
                                ? ""
                                : reader[3]?.ToString() ?? "";

                            modelo.FechaNacimiento = reader[4] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader[4]);

                            modelo.RutaImagen = reader[5] == DBNull.Value
                                ? null
                                : reader[5].ToString();
                        }
                    }
                }
            }

            return View(modelo);
        }

        [HttpPost]
        public IActionResult Formulario(ClienteFormularioAdminModelo modelo)
        {
            if (HttpContext.Session.GetInt32("Rol") != 1)
            {
                return RedirectToAction("Login", "Account");
            }

            string correo =
                modelo.Correo?.Trim().ToLowerInvariant() ?? "";

            if (modelo.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(modelo.Contrasena) ||
                    modelo.Contrasena.Length < 8 ||
                    !modelo.Contrasena.Any(char.IsLetter) ||
                    !modelo.Contrasena.Any(char.IsDigit))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Contrasena),
                        "La contraseña debe tener ocho caracteres, letras y números"
                    );
                }
            }

            if (modelo.FechaNacimiento.HasValue &&
                modelo.FechaNacimiento.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaNacimiento),
                    "No se permiten fechas futuras"
                );
            }

            if (!string.IsNullOrWhiteSpace(modelo.RutaImagen))
            {
                string ruta = modelo.RutaImagen.Trim();

                if (!ruta.StartsWith("~/Imagenes/Clientes/", StringComparison.OrdinalIgnoreCase) ||
                    ruta.Contains("..") ||
                    !(ruta.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                      ruta.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                      ruta.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                      ruta.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(
                        nameof(modelo.RutaImagen),
                        "Escribe una imagen de ~/Imagenes/Clientes/ con extensión .png, .jpg, .jpeg o .webp"
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (SqlConnection con = new Conexion().GetConnection())
            {
                con.Open();

                string sqlCorreo = @"
                    SELECT COUNT(*)
                    FROM Cliente
                    WHERE email = @Email
                    AND id_cliente <> @Id

                    UNION ALL

                    SELECT COUNT(*)
                    FROM Administrador
                    WHERE email = @Email";

                using (SqlCommand cmd = new SqlCommand(sqlCorreo, con))
                {
                    cmd.Parameters.AddWithValue("@Email", correo);
                    cmd.Parameters.AddWithValue("@Id", modelo.Id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int cantidad = 0;

                        while (reader.Read())
                        {
                            cantidad += Convert.ToInt32(reader[0]);
                        }

                        if (cantidad > 0)
                        {
                            ModelState.AddModelError(
                                nameof(modelo.Correo),
                                "Este correo ya está registrado"
                            );
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(modelo);
                }

                string sql = "";

                if (modelo.Id == 0)
                {
                    sql = @"INSERT INTO Cliente
                            (nombre, email, [contraseña],
                             telefono, fecha_nacimiento, ruta_imagen)
                            VALUES
                            (@Nombre, @Email, @Clave,
                             @Telefono, @Fecha, @RutaImagen)";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(modelo.Contrasena))
                    {
                        sql = @"UPDATE Cliente
                                SET nombre = @Nombre,
                                    email = @Email,
                                    telefono = @Telefono,
                                    fecha_nacimiento = @Fecha,
                                    ruta_imagen = @RutaImagen
                                WHERE id_cliente = @Id";
                    }
                    else
                    {
                        sql = @"UPDATE Cliente
                                SET nombre = @Nombre,
                                    email = @Email,
                                    telefono = @Telefono,
                                    fecha_nacimiento = @Fecha,
                                    ruta_imagen = @RutaImagen,
                                    [contraseña] = @Clave
                                WHERE id_cliente = @Id";
                    }
                }

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id);

                    cmd.Parameters.AddWithValue(
                        "@Nombre",
                        modelo.Nombre.Trim()
                    );

                    cmd.Parameters.AddWithValue(
                        "@Email",
                        correo
                    );

                    cmd.Parameters.AddWithValue(
                        "@Telefono",
                        modelo.Telefono.Trim()
                    );

                    cmd.Parameters.AddWithValue(
                        "@Fecha",
                        modelo.FechaNacimiento!.Value.Date
                    );

                    cmd.Parameters.Add("@RutaImagen", System.Data.SqlDbType.NVarChar, 255).Value =
                        string.IsNullOrWhiteSpace(modelo.RutaImagen)
                            ? DBNull.Value
                            : modelo.RutaImagen.Trim();

                    if (!string.IsNullOrWhiteSpace(modelo.Contrasena))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Clave",
                            modelo.Contrasena
                        );
                    }

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            if (HttpContext.Session.GetInt32("Rol") != 1)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using (SqlConnection con =
                    new Conexion().GetConnection())
                {
                    con.Open();

                    string sql = @"DELETE FROM Cliente
                                   WHERE id_cliente = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                TempData["Error"] =
                    "El cliente tiene membresías, reservas u otros registros asociados y no se puede eliminar.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
