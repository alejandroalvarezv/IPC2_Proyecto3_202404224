namespace Backend.Models
{
    public class Pago
    {
        public int CodigoBanco { get; set; }
        public string? Fecha { get; set; }
        public string? NITCliente { get; set; }
        public double Valor { get; set; }
    }
}