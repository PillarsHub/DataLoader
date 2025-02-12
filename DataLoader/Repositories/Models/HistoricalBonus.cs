namespace DataLoader.Repositories.Models
{
    internal class HistoricalBonus
    {
        public long Id { get; set; }
        public string BonusId { get; set; } = string.Empty;
        public long PeriodId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
    }
}
