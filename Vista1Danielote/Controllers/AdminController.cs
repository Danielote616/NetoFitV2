
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class AdminController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        private bool EsAdmin()
        {
            return HttpContext.Session.GetInt32("Rol") == 1;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            DashboardAdminModelo modelo = new DashboardAdminModelo();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Cliente", con))
                {
                    ViewBag.Clientes = Convert.ToInt32(
                        cmd.ExecuteScalar()
                    );
                }

                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT COALESCE(SUM(monto), 0)
                      FROM Pago
                      WHERE estado_pago = 'Confirmado'", con))
                {
                    ViewBag.Ingresos = Convert.ToDecimal(
                        cmd.ExecuteScalar()
                    );
                }

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Asistencia", con))
                {
                    ViewBag.Asistencias = Convert.ToInt32(
                        cmd.ExecuteScalar()
                    );
                }

                string sqlVencer = @"
                    SELECT COUNT(*)
                    FROM Membresias
                    WHERE estado = 'Activa'
                    AND fecha_inicio <= @Hoy
                    AND fecha_fin >= @Hoy
                    AND fecha_fin <= @Limite";

                using (SqlCommand cmd = new SqlCommand(sqlVencer, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Hoy", DateTime.Today);

                    cmd.Parameters.AddWithValue(
                        "@Limite", DateTime.Today.AddDays(15));

                    ViewBag.PorVencer = Convert.ToInt32(
                        cmd.ExecuteScalar()
                    );
                }

                string sqlSucursales = @"
                    SELECT id_sucursal, nombre
                    FROM Sucursal
                    ORDER BY nombre";

                using (SqlCommand cmd = new SqlCommand(sqlSucursales, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ReporteSucursalModelo sucursal =
                                new ReporteSucursalModelo();

                            sucursal.IdSucursal = reader.GetInt32(0);
                            sucursal.Nombre = reader.GetString(1);

                            modelo.Sucursales.Add(sucursal);
                        }
                    }
                }

                int asistenciasIdentificadas = 0;

                foreach (ReporteSucursalModelo sucursal in modelo.Sucursales)
                {
                    string sqlIngresos = @"
                        SELECT COALESCE(SUM(p.monto), 0)
                        FROM Pago p
                        INNER JOIN Membresias m
                            ON m.id_membresia = p.id_membresia
                        WHERE m.id_sucursal = @Sucursal
                        AND p.estado_pago = 'Confirmado'";

                    using (SqlCommand cmd = new SqlCommand(sqlIngresos, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Sucursal", sucursal.IdSucursal);

                        sucursal.Ingresos = Convert.ToDecimal(
                            cmd.ExecuteScalar()
                        );
                    }

                    string sqlAsistencia = @"
                        SELECT COUNT(*)
                        FROM Asistencia a
                        INNER JOIN Reserva r
                            ON r.id_reserva = a.id_reserva
                        INNER JOIN Horario h
                            ON h.id_horario = r.id_horario
                        INNER JOIN Clase c
                            ON c.id_clase = h.id_clase
                        WHERE c.id_sucursal = @Sucursal";

                    using (SqlCommand cmd = new SqlCommand(sqlAsistencia, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Sucursal", sucursal.IdSucursal);

                        sucursal.Asistencias = Convert.ToInt32(
                            cmd.ExecuteScalar()
                        );
                    }

                    asistenciasIdentificadas += sucursal.Asistencias;
                }

                ViewBag.AsistenciasIdentificadas =
                    asistenciasIdentificadas;

                string sqlProximas = @"
                    SELECT c.nombre,
                           COALESCE(p.nombre, 'Sin plan'),
                           COALESCE(s.nombre, 'Sin sucursal'),
                           m.fecha_fin
                    FROM Membresias m
                    INNER JOIN Cliente c
                        ON c.id_cliente = m.id_cliente
                    LEFT JOIN Planes p
                        ON p.id_plan = m.id_plan
                    LEFT JOIN Sucursal s
                        ON s.id_sucursal = m.id_sucursal
                    WHERE m.estado = 'Activa'
                    AND m.fecha_inicio <= @Hoy
                    AND m.fecha_fin >= @Hoy
                    AND m.fecha_fin <= @Limite
                    ORDER BY m.fecha_fin";

                using (SqlCommand cmd = new SqlCommand(sqlProximas, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Hoy", DateTime.Today);

                    cmd.Parameters.AddWithValue(
                        "@Limite", DateTime.Today.AddDays(15));

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ProximaMembresiaModelo proxima =
                                new ProximaMembresiaModelo();

                            proxima.Cliente = reader.GetString(0);
                            proxima.Plan = reader.GetString(1);
                            proxima.Sucursal = reader.GetString(2);
                            proxima.Fecha = reader.GetDateTime(3);

                            modelo.Proximas.Add(proxima);
                        }
                    }
                }
            }

            return View(modelo);
        }

        [HttpGet]
        public IActionResult Sucursales()
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            List<SucursalAdminModelo> lista =
                new List<SucursalAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    SELECT s.id_sucursal,
                           s.nombre,
                           s.ciudad,
                           s.[dirección],
                           s.telefono,
                           s.horario_apertura,
                           s.horario_cierre,
                           (SELECT COUNT(*)
                            FROM Membresias m
                            WHERE m.id_sucursal = s.id_sucursal
                            AND m.estado = 'Activa'
                            AND m.fecha_inicio <= @Hoy
                            AND m.fecha_fin >= @Hoy),
                           s.ruta_imagen
                    FROM Sucursal s
                    ORDER BY s.id_sucursal";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Hoy", DateTime.Today);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SucursalAdminModelo sucursal =
                                new SucursalAdminModelo();

                            sucursal.Id = reader.GetInt32(0);
                            sucursal.Nombre = reader.GetString(1);
                            sucursal.Ciudad = reader.GetString(2);
                            sucursal.Direccion = reader.GetString(3);

                            sucursal.Telefono = reader[4] == DBNull.Value
                                ? ""
                                : reader[4]?.ToString() ?? "";

                            sucursal.Apertura = reader[5] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[5];

                            sucursal.Cierre = reader[6] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[6];

                            sucursal.Miembros = Convert.ToInt32(reader[7]);
                            sucursal.RutaImagen = reader[8] == DBNull.Value
                                ? ""
                                : reader[8]?.ToString() ?? "";

                            lista.Add(sucursal);
                        }
                    }
                }
            }

            return View(
                "~/Views/VistasAdmin/AdminSucursales.cshtml",
                lista
            );
        }

        [HttpGet]
        public IActionResult FormularioSucursal(int? id)
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            SucursalAdminModelo modelo = new SucursalAdminModelo();

            if (id.HasValue)
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();

                    string sql = @"
                        SELECT id_sucursal, nombre, ciudad,
                               [dirección], telefono,
                               horario_apertura, horario_cierre,
                               ruta_imagen
                        FROM Sucursal
                        WHERE id_sucursal = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                return NotFound();

                            modelo.Id = reader.GetInt32(0);
                            modelo.Nombre = reader.GetString(1);
                            modelo.Ciudad = reader.GetString(2);
                            modelo.Direccion = reader.GetString(3);

                            modelo.Telefono = reader[4] == DBNull.Value
                                ? ""
                                : reader[4]?.ToString() ?? "";

                            modelo.Apertura = reader[5] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[5];

                            modelo.Cierre = reader[6] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[6];

                            modelo.RutaImagen = reader[7] == DBNull.Value
                                ? ""
                                : reader[7]?.ToString() ?? "";
                        }
                    }
                }
            }

            return View(modelo);
        }

        [HttpPost]

        public IActionResult FormularioSucursal(SucursalAdminModelo modelo)
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            if (modelo.Cierre <= modelo.Apertura)
            {
                ModelState.AddModelError(
                    "Cierre",
                    "El cierre debe ser posterior a la apertura."
                );
            }

            if (!string.IsNullOrWhiteSpace(modelo.RutaImagen) &&
                !modelo.RutaImagen.Trim().StartsWith("~/Imagenes/Sucursales/"))
            {
                ModelState.AddModelError(
                    "RutaImagen",
                    "La ruta debe empezar por ~/Imagenes/Sucursales/"
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql;

                if (modelo.Id == 0)
                {
                    sql = @"
                        INSERT INTO Sucursal
                        (nombre, ciudad, [dirección], telefono,
                         horario_apertura, horario_cierre, ruta_imagen)
                        VALUES
                        (@Nombre, @Ciudad, @Direccion, @Telefono,
                         @Apertura, @Cierre, @RutaImagen)";
                }
                else
                {
                    sql = @"
                        UPDATE Sucursal
                        SET nombre = @Nombre,
                            ciudad = @Ciudad,
                            [dirección] = @Direccion,
                            telefono = @Telefono,
                            horario_apertura = @Apertura,
                            horario_cierre = @Cierre,
                            ruta_imagen = @RutaImagen
                        WHERE id_sucursal = @Id";
                }

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id);
                    cmd.Parameters.AddWithValue(
                        "@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue(
                        "@Ciudad", modelo.Ciudad.Trim());
                    cmd.Parameters.AddWithValue(
                        "@Direccion", modelo.Direccion.Trim());
                    cmd.Parameters.AddWithValue(
                        "@Telefono", modelo.Telefono.Trim());
                    cmd.Parameters.AddWithValue(
                        "@Apertura", modelo.Apertura);
                    cmd.Parameters.AddWithValue(
                        "@Cierre", modelo.Cierre);
                    cmd.Parameters.Add(
                        "@RutaImagen", System.Data.SqlDbType.NVarChar, 255
                    ).Value = string.IsNullOrWhiteSpace(modelo.RutaImagen)
                        ? DBNull.Value
                        : modelo.RutaImagen.Trim();

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Sucursales");
        }

        [HttpPost]

        public IActionResult EliminarSucursal(int id)
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();

                    string sql = @"
                        DELETE FROM Sucursal
                        WHERE id_sucursal = @Id";

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
                    "No se puede eliminar esta sucursal porque tiene registros asociados.";
            }

            return RedirectToAction("Sucursales");
        }

        [HttpGet]
        public IActionResult Equipos(int? sucursal)
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            List<EquipoAdminModelo> lista =
                new List<EquipoAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    SELECT e.id_equipo,
                           e.nombre,
                           e.tipo,
                           e.estado,
                           e.id_sucursal,
                           s.nombre
                    FROM Equipo e
                    LEFT JOIN Sucursal s
                        ON s.id_sucursal = e.id_sucursal
                    WHERE @Sucursal IS NULL
                       OR e.id_sucursal = @Sucursal
                    ORDER BY e.id_equipo DESC";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add(
                        "@Sucursal",
                        System.Data.SqlDbType.Int
                    ).Value = sucursal.HasValue
                        ? (object)sucursal.Value
                        : DBNull.Value;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EquipoAdminModelo equipo =
                                new EquipoAdminModelo();

                            equipo.Id = reader.GetInt32(0);
                            equipo.Nombre = reader[1]?.ToString() ?? "";
                            equipo.Tipo = reader[2]?.ToString() ?? "";
                            equipo.Estado = reader[3]?.ToString() ?? "";

                            equipo.IdSucursal = reader[4] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader[4]);

                            equipo.Sucursal = reader[5] == DBNull.Value
                                ? "Sin asignar"
                                : reader[5]?.ToString() ?? "";

                            lista.Add(equipo);
                        }
                    }
                }
            }

            ViewBag.Sucursales = LeerSucursales();
            ViewBag.FiltroSucursal = sucursal;

            return View(
                "~/Views/VistasAdmin/AdminEquipos.cshtml",
                lista
            );
        }

        [HttpGet]
        public IActionResult FormularioEquipo(int? id)
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            EquipoAdminModelo modelo = new EquipoAdminModelo();

            if (id.HasValue)
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();

                    string sql = @"
                        SELECT id_equipo, nombre, tipo,
                               estado, id_sucursal
                        FROM Equipo
                        WHERE id_equipo = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                return NotFound();

                            modelo.Id = reader.GetInt32(0);
                            modelo.Nombre = reader[1]?.ToString() ?? "";
                            modelo.Tipo = reader[2]?.ToString() ?? "";
                            modelo.Estado = reader[3]?.ToString() ?? "Operativo";

                            modelo.IdSucursal = reader[4] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader[4]);
                        }
                    }
                }
            }

            ViewBag.Sucursales = LeerSucursales();

            return View(modelo);
        }

        [HttpPost]

        public IActionResult FormularioEquipo(EquipoAdminModelo modelo)
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            if (modelo.Estado != "Operativo" &&
                modelo.Estado != "Mantenimiento" &&
                modelo.Estado != "Dañado")
            {
                ModelState.AddModelError(
                    "Estado",
                    "Estado no válido."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Sucursales = LeerSucursales();
                return View(modelo);
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql;

                if (modelo.Id == 0)
                {
                    sql = @"
                        INSERT INTO Equipo
                        (nombre, tipo, estado, id_sucursal)
                        VALUES
                        (@Nombre, @Tipo, @Estado, @Sucursal)";
                }
                else
                {
                    sql = @"
                        UPDATE Equipo
                        SET nombre = @Nombre,
                            tipo = @Tipo,
                            estado = @Estado,
                            id_sucursal = @Sucursal
                        WHERE id_equipo = @Id";
                }

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id);
                    cmd.Parameters.AddWithValue(
                        "@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue(
                        "@Tipo", modelo.Tipo.Trim());
                    cmd.Parameters.AddWithValue(
                        "@Estado", modelo.Estado);
                    cmd.Parameters.AddWithValue(
                        "@Sucursal", modelo.IdSucursal);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Equipos");
        }

        [HttpPost]

        public IActionResult EliminarEquipo(int id)
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Account");

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    DELETE FROM Equipo
                    WHERE id_equipo = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Equipos");
        }

        private List<SucursalAdminModelo> LeerSucursales()
        {
            List<SucursalAdminModelo> sucursales =
                new List<SucursalAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    SELECT id_sucursal, nombre
                    FROM Sucursal
                    ORDER BY nombre";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SucursalAdminModelo sucursal =
                                new SucursalAdminModelo();

                            sucursal.Id = reader.GetInt32(0);
                            sucursal.Nombre = reader.GetString(1);

                            sucursales.Add(sucursal);
                        }
                    }
                }
            }

            return sucursales;
        }
    }
}
