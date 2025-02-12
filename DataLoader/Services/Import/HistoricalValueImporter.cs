using DataLoader.Repositories;

namespace DataLoader.Services.Import
{
    internal class HistoricalValueImporter
    {
        private readonly HistoricalValueRepository _historicalValueRepository;
        private readonly CSVFileReader _csvFileReader;

        public HistoricalValueImporter(HistoricalValueRepository historicalValueRepository, CSVFileReader sVFileReader)
        {
            _historicalValueRepository = historicalValueRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task Import(string filePath)
        {
            //begindate,enddate,periodId,nodeId,DataPoint,Value

            var data = _csvFileReader.ReadCsvFile(filePath);

            foreach (var row in data)
            {
                long.TryParse(row["periodId"], out long periodId);
                decimal.TryParse(row["Value"], out decimal value);

                await _historicalValueRepository.InsertValue(new Repositories.Models.HistoricalValue
                {
                    Key = row["DataPoint"],
                    NodeId = row["nodeId"],
                    PeriodId = periodId,
                    sumValue = value
                });

                Console.WriteLine($"Imported {row["nodeId"]}");
            }

            Console.WriteLine($"Imported {data.Count} rows");
        }
    }
}
