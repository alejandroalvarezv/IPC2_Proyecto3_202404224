using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml.Linq;

namespace Frontend.Pages.Configuracion
{
    public class RespuestaConfig
    {
        public int ClientesCreados { get; set; }
        public int ClientesActualizados { get; set; }
        public int BancosCreados { get; set; }
        public int BancosActualizados { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public RespuestaConfig? Respuesta { get; set; }
        public string? Error { get; set; }

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                Error = "Por favor selecciona un archivo XML válido.";
                return Page();
            }

            try
            {
                // Leer el archivo
                string xmlContent;
                using (var reader = new StreamReader(archivo.OpenReadStream()))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                // Enviarlo al backend
                var client = _httpClientFactory.CreateClient("Backend");
                var content = new StringContent(xmlContent, 
                              System.Text.Encoding.UTF8, "application/xml");
                var response = await client.PostAsync("/grabarConfiguracion", content);
var responseXml = await response.Content.ReadAsStringAsync();

// Agrega esta línea temporal para ver qué llega
Console.WriteLine("RESPUESTA BACKEND: " + responseXml);

responseXml = responseXml.Trim().TrimStart('\uFEFF', '\u200B');
var doc = XDocument.Parse(responseXml);
                Respuesta = new RespuestaConfig
                {
                    ClientesCreados = int.Parse(doc.Root?
                        .Element("clientes")?.Element("creados")?.Value ?? "0"),
                    ClientesActualizados = int.Parse(doc.Root?
                        .Element("clientes")?.Element("actualizados")?.Value ?? "0"),
                    BancosCreados = int.Parse(doc.Root?
                        .Element("bancos")?.Element("creados")?.Value ?? "0"),
                    BancosActualizados = int.Parse(doc.Root?
                        .Element("bancos")?.Element("actualizados")?.Value ?? "0")
                };
            }
            catch (Exception ex)
            {
                Error = $"Error al procesar el archivo: {ex.Message}";
            }

            return Page();
        }
    }
}