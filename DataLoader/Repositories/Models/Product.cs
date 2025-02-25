namespace DataLoader.Repositories.Models
{
    public class Product
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = null;
        public string? Specifications { get; set; } = null;
        public string? ImageUrl { get; set; } = string.Empty;
        public string[] CategoryIds { get; set; } = Array.Empty<string>();

        public int DisplayIndex { get; set; } = 0;
        public string? UPC { get; set; } = null;
        public string? MPN { get; set; } = null;
        public string? HSCode { get; set; } = null;
        public int? Kit { get; set; } = 0;
        public bool Enabled { get; set; } = true;


        public OutOfStockStatus OutOfStockStatus { get; set; } = OutOfStockStatus.InStock;
        public bool RequiresShipping { get; set; } = false;
        public string? TaxClassId { get; set; } = string.Empty;
        public ProductClass ProductClass { get; set; } = ProductClass.Standard;
    }

    public enum OutOfStockStatus
    {
        InStock = 0,
        OutOfStock = 1,
        PreOrder = 2
    }

    public enum ProductClass
    {
        Standard = 0,
        Material = 1
    }
}
