using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using Backend.Services;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    public class TransaccionController : ControllerBase
    {
        private readonly XmlDataService _dataService = new XmlDataService();

        [HttpPost("grabarTransaccion")]
        public IActionResult GrabarTransaccion()
        {
            string xmlBody;
            using (var reader = new StreamReader(Request.Body))
            {
                xmlBody = reader.ReadToEndAsync().Result;
            }

            var doc = XDocument.Parse(xmlBody);
            var clientes = _dataService.GetClientes();
            var facturas = _dataService.GetFacturas();
            var pagos = _dataService.GetPagos();

            int nuevasFacturas = 0, facturasDuplicadas = 0, facturasConError = 0;
            int nuevosPagos = 0, pagosDuplicados = 0, pagosConError = 0;

            // ——— Procesar Facturas ———
            var facturasXml = doc.Root.Element("facturas")?.Elements("factura")
                              ?? Enumerable.Empty<XElement>();

            foreach (var f in facturasXml)
            {
                var numero = f.Element("numeroFactura")?.Value.Trim();
                var nit = f.Element("NITcliente")?.Value.Trim();
                var fecha = f.Element("fecha")?.Value.Trim();
                var valorStr = f.Element("valor")?.Value.Trim();

                if (facturas.Any(x => x.NumeroFactura == numero))
                {
                    facturasDuplicadas++;
                    continue;
                }

                // Verificar que el cliente existe y el valor es válido
                var cliente = clientes.FirstOrDefault(c => c.NIT == nit);
                if (cliente == null || !double.TryParse(valorStr, out double valor))
                {
                    facturasConError++;
                    continue;
                }

                if (cliente.Saldo < 0)
                {
                    double saldoFavor = Math.Abs(cliente.Saldo);
                    if (saldoFavor >= valor)
                    {
                        cliente.Saldo += valor;
                        valor = 0;
                    }
                    else
                    {
                        valor -= saldoFavor;
                        cliente.Saldo = 0;
                    }
                }

                facturas.Add(new Factura
                {
                    NumeroFactura = numero,
                    NITCliente = nit,
                    Fecha = fecha,
                    Valor = valor,
                    SaldoPendiente = valor
                });
                nuevasFacturas++;
            }

            // ——— Procesar Pagos ———
            var pagosXml = doc.Root.Element("pagos")?.Elements("pago")
                          ?? Enumerable.Empty<XElement>();

            foreach (var p in pagosXml)
            {
                var codigoStr = p.Element("codigoBanco")?.Value.Trim();
                var fecha = p.Element("fecha")?.Value.Trim();
                var nit = p.Element("NITcliente")?.Value.Trim();
                var valorStr = p.Element("valor")?.Value.Trim();

                

                // Verificar datos válidos
                var cliente = clientes.FirstOrDefault(c => c.NIT == nit);
                if (cliente == null || !double.TryParse(valorStr, out double valor)
                    || !int.TryParse(codigoStr, out int codigo))
                {
                    pagosConError++;
                    continue;
                }

                pagos.Add(new Pago
                {
                    CodigoBanco = codigo,
                    Fecha = fecha,
                    NITCliente = nit,
                    Valor = valor
                });
                nuevosPagos++;

                var facturasPendientes = facturas
                    .Where(f => f.NITCliente == nit && f.SaldoPendiente > 0)
                    .OrderBy(f => f.Fecha)
                    .ToList();

                double montoRestante = valor;

                foreach (var factura in facturasPendientes)
                {
                    if (montoRestante <= 0) break;

                    if (montoRestante >= factura.SaldoPendiente)
                    {
                        montoRestante -= factura.SaldoPendiente;
                        factura.SaldoPendiente = 0;
                    }
                    else
                    {
                        factura.SaldoPendiente -= montoRestante;
                        montoRestante = 0;
                    }
                }

                if (montoRestante > 0)
                {
                    cliente.Saldo -= montoRestante;
                }
            }

            // Guardar todo
            _dataService.SaveFacturas(facturas);
            _dataService.SavePagos(pagos);
            _dataService.SaveClientes(clientes);

            // Respuesta XML
            var respuesta = new XDocument(
                new XElement("transacciones",
                    new XElement("facturas",
                        new XElement("nuevasFacturas", nuevasFacturas),
                        new XElement("facturasDuplicadas", facturasDuplicadas),
                        new XElement("facturasConError", facturasConError)
                    ),
                    new XElement("pagos",
                        new XElement("nuevosPagos", nuevosPagos),
                        new XElement("pagosDuplicados", pagosDuplicados),
                        new XElement("pagosConError", pagosConError)
                    )
                )
            );

            return Content(respuesta.ToString(), "application/xml");
        }
    }
}