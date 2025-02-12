namespace DataLoader.Repositories.Models
{
    internal class HistoricalValue
    {
        public string Key { get; set; } = string.Empty;
        public long PeriodId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public decimal? sumValue { get; set; }
        public string? lastValue { get; set; }
    }
}
