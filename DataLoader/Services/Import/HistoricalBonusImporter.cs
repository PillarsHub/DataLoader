using DataLoader.Repositories;

namespace DataLoader.Services.Import
{
    internal class HistoricalBonusImporter
    {
        private readonly HistoricalBonusRepository _historicalBonusRepository;
        private readonly CSVFileReader _csvFileReader;

        public HistoricalBonusImporter(HistoricalBonusRepository historicalValueRepository, CSVFileReader sVFileReader)
        {
            _historicalBonusRepository = historicalValueRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task Import(string filePath)
        {
            //bonusid, begindate, enddate, periodId, nodeId, total_amount
            var data = _csvFileReader.ReadCsvFile(filePath);
            var errors = new List<object>();

            foreach (var row in data)
            {
                long.TryParse(row["periodId"], out long periodId);
                decimal.TryParse(row["total_amount"], out decimal value);

                var bonus = new Repositories.Models.HistoricalBonus
                {
                    BonusId = row["bonusid"],
                    NodeId = row["nodeId"],
                    PeriodId = periodId,
                    Amount = value
                };

                try
                {
                    await _historicalBonusRepository.InsertValue(bonus);
                }
                catch (Exception ex)
                {
                    errors.Add(new { error = ex.Message, bonus });
                }

                Console.WriteLine($"Imported {row["nodeId"]}");
            }

            Console.WriteLine($"Imported {data.Count} rows");
        }
    }
}