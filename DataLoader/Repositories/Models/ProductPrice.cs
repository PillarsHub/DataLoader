using System.Text.Json.Serialization;

namespace DataLoader.Repositories.Models
{
    public class ProductPrice
    {
        [JsonInclude]
        public string Id { get; internal set; } = string.Empty;
        [JsonInclude]
        public string ProductId { get; internal set; } = string.Empty;
        public decimal Price { get; set; } = 0;
        public string PriceCurrency { get; set; } = "usd";
        public PriceVolume[] Volume { get; set; } = Array.Empty<PriceVolume>();

        public DateTime Start { get; set; } = DateTime.UtcNow;
        public DateTime? End { get; set; } = null;
        public PriceType PriceType { get; set; } = PriceType.Price;

        public string[] StoreIds { get; set; } = Array.Empty<string>();
        public string[] PriceGroups { get; set; } = Array.Empty<string>();
        public string[] RegionIds { get; set; } = Array.Empty<string>();
        public string[] OrderTypeIds { get; set; } = Array.Empty<string>();

        public object? CustomData { get; set; } = null;
    }

    public enum PriceType
    {
        Price = 0,
        Discount = 1
    }

    public class PriceVolume
    {
        public string VolumeId { get; set; } = string.Empty;
        public decimal Volume { get; set; } = 0;
    }
}
