using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    public class PeticionesController : ControllerBase
    {
        private readonly XmlDataService _dataService = new XmlDataService();


        [HttpGet("devolverEstadoCuenta")]
        public IActionResult DevolverEstadoCuenta([FromQuery] string? nit = null)
        {
            var clientes = _dataService.GetClientes();
            var facturas = _dataService.GetFacturas();
            var pagos = _dataService.GetPagos();
            var bancos = _dataService.GetBancos();

            if (!string.IsNullOrEmpty(nit))
                clientes = clientes.Where(c => c.NIT == nit).ToList();

            // Ordenar por NIT
            clientes = clientes.OrderBy(c => c.NIT).ToList();

            var clientesXml = clientes.Select(cliente =>
            {
                // Obtener transacciones del cliente (facturas + pagos)
                var facturasCliente = facturas
                    .Where(f => f.NITCliente == cliente.NIT)
                    .Select(f => new
                    {
                        Fecha = f.Fecha,
                        Tipo = "cargo",
                        Valor = f.Valor,
                        Referencia = $"Fact. # {f.NumeroFactura}"
                    });

                var pagosCliente = pagos
                    .Where(p => p.NITCliente == cliente.NIT)
                    .Select(p => new
                    {
                        Fecha = p.Fecha,
                        Tipo = "abono",
                        Valor = p.Valor,
                        Referencia = bancos.FirstOrDefault(b => 
                            b.Codigo == p.CodigoBanco)?.Nombre ?? "Banco desconocido"
                    });

                // Unir y ordenar de más reciente a más antigua
                var transacciones = facturasCliente
                    .Concat(pagosCliente)
                    .OrderByDescending(t => t.Fecha)
                    .ToList();

                return new XElement("cliente",
                    new XElement("NIT", cliente.NIT),
                    new XElement("nombre", cliente.Nombre),
                    new XElement("saldo", cliente.Saldo),
                    new XElement("transacciones",
                        transacciones.Select(t => new XElement("transaccion",
                            new XElement("fecha", t.Fecha),
                            new XElement("tipo", t.Tipo),
                            new XElement("valor", t.Valor),
                            new XElement("referencia", t.Referencia)
                        ))
                    )
                );
            });

            var respuesta = new XDocument(
                new XElement("estadoCuenta", clientesXml)
            );

            return Content(respuesta.ToString(), "application/xml");
        }

        [HttpGet("devolverResumenPagos")]
        public IActionResult DevolverResumenPagos([FromQuery] int mes, [FromQuery] int anio)
        {
            var pagos = _dataService.GetPagos();
            var bancos = _dataService.GetBancos();

            // Obtener los últimos 3 meses
            var meses = new List<(int Mes, int Anio)>();
            for (int i = 0; i < 3; i++)
            {
                meses.Add((mes, anio));
                mes--;
                if (mes == 0) { mes = 12; anio--; }
            }

            var bancosXml = bancos.Select(banco =>
            {
                var pagosPorMes = meses.Select(m =>
                {
                    var total = pagos
                        .Where(p => p.CodigoBanco == banco.Codigo)
                        .Where(p => {
                            var partes = p.Fecha?.Split('/');
                            if (partes?.Length == 3)
                                return int.Parse(partes[1]) == m.Mes 
                                    && int.Parse(partes[2]) == m.Anio;
                            return false;
                        })
                        .Sum(p => p.Valor);

                    return new XElement("mes",
                        new XElement("numero", m.Mes),
                        new XElement("anio", m.Anio),
                        new XElement("total", total)
                    );
                });

                return new XElement("banco",
                    new XElement("codigo", banco.Codigo),
                    new XElement("nombre", banco.Nombre),
                    new XElement("pagos", pagosPorMes)
                );
            });

            var respuesta = new XDocument(
                new XElement("resumenPagos", bancosXml)
            );

            return Content(respuesta.ToString(), "application/xml");
        }
    }
}