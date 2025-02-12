using DataLoader.Repositories;
using System.Globalization;

namespace DataLoader.Services.Import
{
    internal class SourceImporter
    {
        private readonly CustomerRepository _customerRepository;
        private readonly CSVFileReader _csvFileReader;

        public SourceImporter(CustomerRepository customerRepository, CSVFileReader sVFileReader)
        {
            _customerRepository = customerRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task Import(string filePath)
        {
            var timeZoneId = "Pacific Standard Time";
            var dateFormat = "M/d/yyyy";

            var data = _csvFileReader.ReadCsvFile(filePath);

            foreach (var row in data)
            {
                var nodeId = row["NodeId"];
                var comment = row["Comment"];
                var amt = row["Amt"];

                decimal.TryParse(amt.Replace("$", ""), out decimal amount);

                var sourceId = "MBonus";
                try
                {
                    await _customerRepository.InsertSource(new Sources.Source
                    {
                        Date = new DateTime(2024, 11, 30, 0, 0, 0, DateTimeKind.Utc),
                        ExternalId = comment,
                        NodeId = nodeId,
                        SourceGroupId = sourceId,
                        Value = amount
                    });
                }
                catch (Exception ex)
                {
                    int rr = 0;
                }

                Console.WriteLine($"Imported {nodeId}");
            }

            Console.WriteLine($"Imported {data.Count} rows");
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
