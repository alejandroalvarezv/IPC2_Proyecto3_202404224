using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using Backend.Services;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    public class ConfiguracionController : ControllerBase
    {
        private readonly XmlDataService _dataService = new XmlDataService();

        // POST /limpiarDatos
        [HttpPost("limpiarDatos")]
        public IActionResult LimpiarDatos()
        {
            _dataService.LimpiarDatos();
            return Ok("<respuesta>Datos eliminados correctamente</respuesta>");
        }

        // POST /grabarConfiguracion
        [HttpPost("grabarConfiguracion")]
        public IActionResult GrabarConfiguracion()
        {
            // Leer el XML del body
            string xmlBody;
            using (var reader = new StreamReader(Request.Body))
            {
                xmlBody = reader.ReadToEndAsync().Result;
            }

            var doc = XDocument.Parse(xmlBody);

            // ——— Procesar Clientes ———
            var clientesActuales = _dataService.GetClientes();
            int clientesCreados = 0, clientesActualizados = 0;

            var clientesXml = doc.Root.Element("clientes")?.Elements("cliente") 
                              ?? Enumerable.Empty<XElement>();

            foreach (var c in clientesXml)
            {
                var nit = c.Element("NIT")?.Value.Trim();
                var nombre = c.Element("nombre")?.Value.Trim();
                var existente = clientesActuales.FirstOrDefault(x => x.NIT == nit);

                if (existente == null)
                {
                    clientesActuales.Add(new Cliente { NIT = nit, Nombre = nombre, Saldo = 0 });
                    clientesCreados++;
                }
                else
                {
                    existente.Nombre = nombre;
                    clientesActualizados++;
                }
            }
            _dataService.SaveClientes(clientesActuales);

            // ——— Procesar Bancos ———
            var bancosActuales = _dataService.GetBancos();
            int bancosCreados = 0, bancosActualizados = 0;

            var bancosXml = doc.Root.Element("bancos")?.Elements("banco") 
                           ?? Enumerable.Empty<XElement>();

            foreach (var b in bancosXml)
            {
                var codigo = int.Parse(b.Element("codigo")?.Value.Trim() ?? "0");
                var nombre = b.Element("nombre")?.Value.Trim();
                var existente = bancosActuales.FirstOrDefault(x => x.Codigo == codigo);

                if (existente == null)
                {
                    bancosActuales.Add(new Banco { Codigo = codigo, Nombre = nombre });
                    bancosCreados++;
                }
                else
                {
                    existente.Nombre = nombre;
                    bancosActualizados++;
                }
            }
            _dataService.SaveBancos(bancosActuales);

            // ——— Generar respuesta XML ———
            var respuesta = new XDocument(
                new XElement("respuesta",
                    new XElement("clientes",
                        new XElement("creados", clientesCreados),
                        new XElement("actualizados", clientesActualizados)
                    ),
                    new XElement("bancos",
                        new XElement("creados", bancosCreados),
                        new XElement("actualizados", bancosActualizados)
                    )
                )
            );

            return Content(respuesta.ToString(), "application/xml");
        }
    }
}