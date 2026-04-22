using System.Xml.Linq;
using Backend.Models;

namespace Backend.Services
{
    public class XmlDataService
    {
        private readonly string _basePath = "Data";

        public XmlDataService()
        {
            Directory.CreateDirectory(_basePath);
        }

        // ——— CLIENTES ———
        public List<Cliente> GetClientes()
        {
            var path = Path.Combine(_basePath, "clientes.xml");
            if (!File.Exists(path)) return new List<Cliente>();

            var doc = XDocument.Load(path);
            return doc.Root.Elements("cliente").Select(e => new Cliente
            {
                NIT = e.Element("NIT")?.Value.Trim(),
                Nombre = e.Element("nombre")?.Value.Trim(),
                Saldo = double.Parse(e.Element("saldo")?.Value ?? "0")
            }).ToList();
        }

        public void SaveClientes(List<Cliente> clientes)
        {
            var path = Path.Combine(_basePath, "clientes.xml");
            var doc = new XDocument(new XElement("clientes",
                clientes.Select(c => new XElement("cliente",
                    new XElement("NIT", c.NIT),
                    new XElement("nombre", c.Nombre),
                    new XElement("saldo", c.Saldo)
                ))
            ));
            doc.Save(path);
        }

        // ——— BANCOS ———
        public List<Banco> GetBancos()
        {
            var path = Path.Combine(_basePath, "bancos.xml");
            if (!File.Exists(path)) return new List<Banco>();

            var doc = XDocument.Load(path);
            return doc.Root.Elements("banco").Select(e => new Banco
            {
                Codigo = int.Parse(e.Element("codigo")?.Value ?? "0"),
                Nombre = e.Element("nombre")?.Value.Trim()
            }).ToList();
        }

        public void SaveBancos(List<Banco> bancos)
        {
            var path = Path.Combine(_basePath, "bancos.xml");
            var doc = new XDocument(new XElement("bancos",
                bancos.Select(b => new XElement("banco",
                    new XElement("codigo", b.Codigo),
                    new XElement("nombre", b.Nombre)
                ))
            ));
            doc.Save(path);
        }

        // ——— LIMPIAR TODO ———
        public void LimpiarDatos()
        {
            var archivos = new[] { "clientes.xml", "bancos.xml", "facturas.xml", "pagos.xml" };
            foreach (var archivo in archivos)
            {
                var path = Path.Combine(_basePath, archivo);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}