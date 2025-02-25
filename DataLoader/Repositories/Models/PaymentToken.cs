using System.ComponentModel.DataAnnotations;

namespace DataLoader.Repositories.Models
{
    internal class PaymentToken
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ProfileType { get; set; }
        public string? Input1 { get; set; }
        public string? Input2 { get; set; }
        public string? Input3 { get; set; }
        public string? Input4 { get; set; }

        [MaxLength(4)]
        public string? Last4CC { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public string Token { get; set; } = string.Empty;
        public ShipAddress? Address { get; set; }
    }
}
