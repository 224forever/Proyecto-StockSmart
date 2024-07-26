using System.ComponentModel.DataAnnotations;

namespace StockSmart.Models
{
    public class Producto
    {
        [Display(Name = "ID del Producto")]
        public string ProductID { get; set; }

        [Display(Name = "Nombre del Producto")]
        public string ProductName { get; set; }

        [Display(Name = "ID del Proveedor")]
        public string SupplierID { get; set; }

        [Display(Name = "ID de la Categoría")]
        public string CategoryID { get; set; }

        [Display(Name = "Cantidad por Unidad")]
        public string QuantityPerUnit { get; set; }

        [Display(Name = "Precio Unitario")]
        public string UnitPrice { get; set; }

        [Display(Name = "Unidades en Stock")]
        public string UnitsInStock { get; set; }

        [Display(Name = "Unidades en Pedido")]
        public string UnitsOnOrder { get; set; }

        [Display(Name = "Nivel de Reorden")]
        public string ReorderLevel { get; set; }

        [Display(Name = "Descontinuado")]
        public string Discontinued { get; set; }
    }
}
