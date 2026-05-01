using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml.Linq;

namespace Frontend.Pages.Transacciones
{
    public class RespuestaTransac
    {
        public int NuevasFacturas { get; set; }
        public int FacturasDuplicadas { get; set; }
        public int FacturasConError { get; set; }
        public int NuevosPagos { get; set; }
        public int PagosDuplicados { get; set; }
        public int PagosConError { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public RespuestaTransac? Respuesta { get; set; }
        public string? Error { get; set; }
        public string? RespuestaXml { get; set; }

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
                string xmlContent;
                using (var reader = new StreamReader(archivo.OpenReadStream()))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                var client = _httpClientFactory.CreateClient("Backend");
                var content = new StringContent(xmlContent,
                              System.Text.Encoding.UTF8, "application/xml");
                var response = await client.PostAsync("/grabarTransaccion", content);
                var responseXml = await response.Content.ReadAsStringAsync();

                responseXml = responseXml.Trim().TrimStart('\uFEFF', '\u200B');
                RespuestaXml = responseXml;

                var doc = XDocument.Parse(responseXml);

                Respuesta = new RespuestaTransac
                {
                    NuevasFacturas = int.Parse(doc.Root?
                        .Element("facturas")?.Element("nuevasFacturas")?.Value ?? "0"),
                    FacturasDuplicadas = int.Parse(doc.Root?
                        .Element("facturas")?.Element("facturasDuplicadas")?.Value ?? "0"),
                    FacturasConError = int.Parse(doc.Root?
                        .Element("facturas")?.Element("facturasConError")?.Value ?? "0"),
                    NuevosPagos = int.Parse(doc.Root?
                        .Element("pagos")?.Element("nuevosPagos")?.Value ?? "0"),
                    PagosDuplicados = int.Parse(doc.Root?
                        .Element("pagos")?.Element("pagosDuplicados")?.Value ?? "0"),
                    PagosConError = int.Parse(doc.Root?
                        .Element("pagos")?.Element("pagosConError")?.Value ?? "0")
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