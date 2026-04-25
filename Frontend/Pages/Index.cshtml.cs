using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public string? Mensaje { get; set; }

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostResetAsync()
        {
            var client = _httpClientFactory.CreateClient("Backend");
            await client.PostAsync("/limpiarDatos", null);
            Mensaje = "Datos reseteados correctamente";
            return Page();
        }
    }
}