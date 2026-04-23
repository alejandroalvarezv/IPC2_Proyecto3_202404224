namespace Backend.Models
{
    public class Factura
    {
        public string? NumeroFactura { get; set; }
        public string? NITCliente { get; set; }
        public string? Fecha { get; set; }
        public double Valor { get; set; }
        public double SaldoPendiente { get; set; }
    }
}