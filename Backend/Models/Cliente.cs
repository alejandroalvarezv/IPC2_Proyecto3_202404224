namespace Backend.Models
{
    public class Cliente
    {
        public string? NIT { get; set; }
        public string ?Nombre { get; set; }
        public double Saldo { get; set; } = 0;
    }
}