using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using System.Globalization;

namespace DataLoader.Services.Import
{
    internal class HistoricalBonusImporter
    {
        private readonly HistoricalBonusRepository _historicalBonusRepository;
        private readonly CustomerRepository _periodRepository;
        private readonly CSVFileReader _csvFileReader;

        public HistoricalBonusImporter(HistoricalBonusRepository historicalValueRepository, CSVFileReader sVFileReader, CustomerRepository periodRepository)
        {
            _historicalBonusRepository = historicalValueRepository;
            _csvFileReader = sVFileReader;
            _periodRepository = periodRepository;
        }

        public async Task Import(string filePath)
        {
            var timeZoneId = "Mountain Standard Time";
            var dateFormat = "M/d/yyyy H:mm";

            //bonusId, beginDate, endDate, nodeId, amount, postDate
            var data = _csvFileReader.ReadCsvFile(filePath);
            var bonuses = new List<HistoricalBonus>();
            var errors = new List<object>();

            foreach (var row in data)
            {
                decimal.TryParse(row["amount"], out decimal value);
                var date = ReadDate(row["endDate"], dateFormat, timeZoneId);
                if (date == null) break;

                //var period = await _periodRepository.GetPeriod(date.Value.AddMinutes(-1));
                //if (period == null) break;

                bonuses.Add(new HistoricalBonus
                {
                    BonusId = row["bonusId"],
                    NodeId = row["nodeId"],
                    //PeriodId = period.Id,
                    PeriodDate = date,
                    Amount = value
                });
            }

            var minDate = bonuses.MinBy(x => x.PeriodDate);
            var maxDate = bonuses.MaxBy(x => x.PeriodDate);

            var dateGroup = bonuses.GroupBy(b => b.PeriodDate.HasValue ? b.PeriodDate.Value.Date : DateTime.Now.Date);
            foreach (var group in dateGroup)
            {
                Console.WriteLine($"Loading period for {group.Key}");
                var period = await _periodRepository.GetPeriod(group.Key);
                if (period == null) continue;
                foreach (var item in group)
                {
                    item.PeriodId = period.Id;
                }
            }

            List<object> ErrorList = new List<object>();

            await Parallel.ForEachAsync(bonuses, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (bonus, cancellationToken) =>
            {
                try
                {
                    await _historicalBonusRepository.InsertValue(bonus);
                    Console.WriteLine($"Imported {bonus.BonusId} {bonus.NodeId}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new { msg = ex.Message, bonus = bonus });
                    }
                }
            });

            Console.WriteLine($"Imported {bonuses.Count} rows");
        }

        private DateTime? ReadDate(string date, string dateFormat, string timeZoneId)
        {
            if (string.IsNullOrEmpty(date)) return null;

            if (DateTime.TryParseExact(date, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime orderDate))
            {
                // Specify the mountain time zone
                TimeZoneInfo mountainTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

                // Convert to UTC
                DateTime utcOrderDate = TimeZoneInfo.ConvertTimeToUtc(orderDate, mountainTimeZone);

                return utcOrderDate;
            }
            else
            {
                throw new Exception("Failed to parse order date.");
            }
        }
    }
}