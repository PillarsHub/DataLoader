namespace Importer.Contracts;

public class OrderVolumeImportRequest
{
    public string OrderId { get; set; } = "";
    public OrderLineItemVolume[]? Volume { get; set; }
}

public class OrderLineItemVolume
{
    public string VolumeId { get; set; } = "";
    public decimal Volume { get; set; }
}

