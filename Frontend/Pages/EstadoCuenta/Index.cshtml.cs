using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml.Linq;

namespace Frontend.Pages.EstadoCuenta
{
    public class Transaccion
    {
        public string Fecha { get; set; } = "";
        public string Tipo { get; set; } = "";
        public double Valor { get; set; }
        public string Referencia { get; set; } = "";
    }

    public class ClienteEstado
    {
        public string NIT { get; set; } = "";
        public string Nombre { get; set; } = "";
        public double Saldo { get; set; }
        public List<Transaccion> Transacciones { get; set; } = new();
    }

    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public List<ClienteEstado>? Clientes { get; set; }
        public string? Error { get; set; }
        public string? NitBuscado { get; set; }

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(string? nit)
        {
            NitBuscado = nit;
            try
            {
                var client = _httpClientFactory.CreateClient("Backend");
                var url = string.IsNullOrEmpty(nit)
                    ? "/devolverEstadoCuenta"
                    : $"/devolverEstadoCuenta?nit={nit}";

                var response = await client.GetAsync(url);
                var responseXml = await response.Content.ReadAsStringAsync();
                responseXml = responseXml.Trim().TrimStart('\uFEFF', '\u200B');

                var doc = XDocument.Parse(responseXml);
                Clientes = doc.Root?.Elements("cliente").Select(c => new ClienteEstado
                {
                    NIT = c.Element("NIT")?.Value ?? "",
                    Nombre = c.Element("nombre")?.Value ?? "",
                    Saldo = double.Parse(c.Element("saldo")?.Value ?? "0"),
                    Transacciones = c.Element("transacciones")?
                        .Elements("transaccion").Select(t => new Transaccion
                        {
                            Fecha = t.Element("fecha")?.Value ?? "",
                            Tipo = t.Element("tipo")?.Value ?? "",
                            Valor = double.Parse(t.Element("valor")?.Value ?? "0"),
                            Referencia = t.Element("referencia")?.Value ?? ""
                        }).ToList() ?? new List<Transaccion>()
                }).ToList() ?? new List<ClienteEstado>();
            }
            catch (Exception ex)
            {
                Error = $"Error al consultar: {ex.Message}";
            }

            return Page();
        }
    }
}