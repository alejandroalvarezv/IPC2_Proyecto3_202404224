using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using Backend.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;



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

        [HttpGet("generarPdfEstadoCuenta")]
        public IActionResult GenerarPdfEstadoCuenta([FromQuery] string? nit = null)
    {
    var clientes = _dataService.GetClientes();
    var facturas = _dataService.GetFacturas();
    var pagos = _dataService.GetPagos();
    var bancos = _dataService.GetBancos();

    if (!string.IsNullOrEmpty(nit))
        clientes = clientes.Where(c => c.NIT == nit).ToList();

    clientes = clientes.OrderBy(c => c.NIT).ToList();

    var pdf = QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(50);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Text("ITGSA - Estado de Cuenta")
                .SemiBold().FontSize(18).FontColor("#1a237e");

            page.Content().Column(col =>
            {
                foreach (var cliente in clientes)
                {
                    col.Item().PaddingTop(15).Text($"Cliente: {cliente.NIT} — {cliente.Nombre}")
                        .SemiBold().FontSize(12).FontColor("#1a237e");

                    col.Item().Text($"Saldo actual: Q. {cliente.Saldo:F2}");

                    var facturasCliente = facturas
                        .Where(f => f.NITCliente == cliente.NIT)
                        .Select(f => new {
                            f.Fecha, Tipo = "cargo",
                            f.Valor, Referencia = $"Fact. # {f.NumeroFactura}"
                        });

                    var pagosCliente = pagos
                        .Where(p => p.NITCliente == cliente.NIT)
                        .Select(p => new {
                            p.Fecha, Tipo = "abono",
                            p.Valor,
                            Referencia = bancos.FirstOrDefault(b =>
                                b.Codigo == p.CodigoBanco)?.Nombre ?? "Banco"
                        });

                    var transacciones = facturasCliente
                        .Concat(pagosCliente)
                        .OrderByDescending(t => t.Fecha)
                        .ToList();

                    if (transacciones.Count == 0)
                    {
                        col.Item().PaddingTop(5).Text("Sin transacciones registradas.")
                            .Italic().FontColor("#888888");
                        continue;
                    }

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background("#1a237e")
                                .Padding(5).Text("Fecha").FontColor("#ffffff").SemiBold();
                            header.Cell().Background("#1a237e")
                                .Padding(5).Text("Cargo").FontColor("#ffffff").SemiBold();
                            header.Cell().Background("#1a237e")
                                .Padding(5).Text("Abono").FontColor("#ffffff").SemiBold();
                            header.Cell().Background("#1a237e")
                                .Padding(5).Text("Referencia").FontColor("#ffffff").SemiBold();
                        });

                        // Rows
                        foreach (var t in transacciones)
                        {
                            table.Cell().Padding(4).Text(t.Fecha);
                            table.Cell().Padding(4).Text(
                                t.Tipo == "cargo" ? $"Q. {t.Valor:F2}" : "");
                            table.Cell().Padding(4).Text(
                                t.Tipo == "abono" ? $"Q. {t.Valor:F2}" : "");
                            table.Cell().Padding(4).Text(t.Referencia);
                        }
                    });

                    col.Item().PaddingTop(5)
                        .LineHorizontal(1).LineColor("#dddddd");
                }
            });

            page.Footer().AlignCenter()
                .Text(x =>
                {
                    x.Span("Generado el ");
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                });
        });
    });

    var pdfBytes = pdf.GeneratePdf();
    return File(pdfBytes, "application/pdf", "EstadoCuenta.pdf");
}



        [HttpGet("generarPdfIngresos")]
    public IActionResult GenerarPdfIngresos([FromQuery] int mes, [FromQuery] int anio)
{
    var pagos = _dataService.GetPagos();
    var bancos = _dataService.GetBancos();

    var meses = new List<(int Mes, int Anio)>();
    int mesTemp = mes, anioTemp = anio;
    for (int i = 0; i < 3; i++)
    {
        meses.Add((mesTemp, anioTemp));
        mesTemp--;
        if (mesTemp == 0) { mesTemp = 12; anioTemp--; }
    }

    string[] nombresMeses = {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    var pdf = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(50);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Text("ITGSA - Reporte de Ingresos por Banco")
                    .SemiBold().FontSize(18).FontColor("#1a237e");
                col.Item().Text(
                    $"Período: {nombresMeses[meses[2].Mes - 1]}/{meses[2].Anio} " +
                    $"— {nombresMeses[meses[0].Mes - 1]}/{meses[0].Anio}")
                    .FontSize(11).FontColor("#555555");
            });

            page.Content().PaddingTop(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#1a237e")
                        .Padding(5).Text("Banco")
                        .FontColor("#ffffff").SemiBold();
                    foreach (var m in meses)
                    {
                        header.Cell().Background("#1a237e")
                            .Padding(5)
                            .Text($"{nombresMeses[m.Mes - 1].Substring(0, 3)}-{m.Anio.ToString().Substring(2)}")
                            .FontColor("#ffffff").SemiBold();
                    }
                });

                // Rows
                bool alternado = false;
                foreach (var banco in bancos)
                {
                    var bg = alternado ? "#f5f5f5" : "#ffffff";
                    alternado = !alternado;

                    table.Cell().Background(bg).Padding(5).Text(banco.Nombre);

                    foreach (var m in meses)
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

                        table.Cell().Background(bg).Padding(5)
                            .Text($"Q. {total:F2}");
                    }
                }

                // Fila de totales
                table.Cell().Background("#1a237e")
                    .Padding(5).Text("TOTAL")
                    .FontColor("#ffffff").SemiBold();

                foreach (var m in meses)
                {
                    var totalMes = pagos
                        .Where(p => {
                            var partes = p.Fecha?.Split('/');
                            if (partes?.Length == 3)
                                return int.Parse(partes[1]) == m.Mes
                                    && int.Parse(partes[2]) == m.Anio;
                            return false;
                        })
                        .Sum(p => p.Valor);

                    table.Cell().Background("#1a237e")
                        .Padding(5).Text($"Q. {totalMes:F2}")
                        .FontColor("#ffffff").SemiBold();
                }
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generado el ");
                x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            });
        });
    });

    var pdfBytes = pdf.GeneratePdf();
    return File(pdfBytes, "application/pdf", "IngresosPorBanco.pdf");
}

    }
    
}