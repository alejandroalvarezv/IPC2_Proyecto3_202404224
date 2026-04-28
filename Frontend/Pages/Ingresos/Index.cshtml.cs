using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml.Linq;

namespace Frontend.Pages.Ingresos
{
    public class BancoResumen
    {
        public string Nombre { get; set; } = "";
        public List<double> Totales { get; set; } = new();
    }

    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public List<BancoResumen>? Bancos { get; set; }
        public List<string> Meses { get; set; } = new();
        public string? Error { get; set; }
        public int Mes { get; set; } = DateTime.Now.Month;
        public int Anio { get; set; } = DateTime.Now.Year;
        public string NombreMes { get; set; } = "";

        private readonly string[] _nombresMeses = {
            "ene", "feb", "mar", "abr", "may", "jun",
            "jul", "ago", "sep", "oct", "nov", "dic"
        };

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(int mes, int anio)
        {
            Mes = mes;
            Anio = anio;
            NombreMes = _nombresMeses[mes - 1];

            try
            {
                var client = _httpClientFactory.CreateClient("Backend");
                var response = await client.GetAsync(
                    $"/devolverResumenPagos?mes={mes}&anio={anio}");
                var responseXml = await response.Content.ReadAsStringAsync();
                responseXml = responseXml.Trim().TrimStart('\uFEFF', '\u200B');

                var doc = XDocument.Parse(responseXml);

                // Calcular los 3 meses a mostrar
                for (int i = 0; i < 3; i++)
                {
                    int m = mes - i;
                    int a = anio;
                    if (m <= 0) { m += 12; a--; }
                    Meses.Add($"{_nombresMeses[m - 1]}-{a.ToString().Substring(2)}");
                }

                Bancos = doc.Root?.Elements("banco").Select(b => new BancoResumen
                {
                    Nombre = b.Element("nombre")?.Value ?? "",
                    Totales = b.Element("pagos")?.Elements("mes")
                        .Select(m => double.Parse(m.Element("total")?.Value ?? "0"))
                        .ToList() ?? new List<double>()
                }).ToList() ?? new List<BancoResumen>();
            }
            catch (Exception ex)
            {
                Error = $"Error al consultar: {ex.Message}";
            }

            return Page();
        }
    }
}