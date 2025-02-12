namespace DataLoader.Repositories.Models
{
    internal class Order
    {
        public string Id { get; set; } = string.Empty;
        //public List<string> ExternalIds { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? OrderType { get; set; } = string.Empty;
        public decimal SubTotal { get; set; } = 0;
        public decimal Discount { get; set; } = 0;
        public decimal Shipping { get; set; } = 0;
        public decimal Tax { get; set; } = 0;
        public decimal TaxRate { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string? Status { get; set; } = string.Empty;
        public string? Tracking { get; set; } = string.Empty;
        public string? Notes { get; set; } = string.Empty;
        public ShipAddress? ShipAddress { get; set; }
        //public List<Payment> Payments { get; set; } = string.Empty;
        public OrderLineItem[]? LineItems { get; set; } = Array.Empty<OrderLineItem>();
        //public List<TaxDetail> TaxDetails { get; set; } = string.Empty;
    }

    internal class ShipAddress
    {
        public string Line1 { get; set; } = string.Empty;
        public string Line2 { get; set; } = string.Empty;
        public string Line3 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }

    //internal class Payment
    //{
    //    public decimal Amount { get; set; }
    //    public string AuthorizationNumber { get; set; }
    //    public string TransactionNumber { get; set; }
    //    public DateTime Date { get; set; }
    //    public string Merchant { get; set; }
    //    public string Response { get; set; }
    //    public string PaymentType { get; set; }
    //    public string Reference { get; set; }
    //    public string Status { get; set; }
    //}

    //internal class TaxDetail
    //{
    //    public decimal TaxRate { get; set; }
    //    public decimal TaxAmount { get; set; }
    //    public string TaxClass { get; set; }
    //    public string AuthorityLevel { get; set; }
    //    public string AuthorityName { get; set; }
    //}

}
