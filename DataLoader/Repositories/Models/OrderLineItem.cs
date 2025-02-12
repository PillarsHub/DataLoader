namespace DataLoader.Repositories.Models
{
    internal class OrderLineItem
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public LineItemVolume[]? Volume { get; set; } = Array.Empty<LineItemVolume>();
    }
    internal class LineItemVolume
    {
        public string VolumeId { get; set; } = string.Empty;
        public decimal Volume { get; set; }
    }
}
