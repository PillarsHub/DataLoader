
namespace Importer.Contracts;
public class OrderLineItemImportRequest
{
    public string OrderId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public OrderLineItemVolume[]? Volume { get; set; }
}

