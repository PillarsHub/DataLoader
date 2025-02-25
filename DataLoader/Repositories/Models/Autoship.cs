namespace DataLoader.Repositories.Models
{
    internal class Autoship
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; internal set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public Frequency Frequency { get; set; } = Frequency.Monthly;
        public string? AutoshipType { get; set; } = string.Empty;
        public string? ShippingMethod { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; } = string.Empty;
        public string? Status { get; set; } = string.Empty;
        public DateTime? LastAutoshipDate { get; set; }
        public DateTime? NextAutoshipDate { get; set; }
        public ShipAddress Address { get; set; } = new ShipAddress();
        public AutoshipLineItem[] LineItems { get; set; } = Array.Empty<AutoshipLineItem>();
    }

    public enum Frequency
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        Quarterly = 3,
        Yearly = 4,
    }

    public class AutoshipLineItem
    {
        public string AutoshipId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 0;
    }
}
