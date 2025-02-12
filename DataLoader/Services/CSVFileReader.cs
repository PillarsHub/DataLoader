using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace DataLoader.Services
{
    internal class CSVFileReader
    {
        public List<Dictionary<string, string>> ReadCsvFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            });

            var records = new List<Dictionary<string, string>>();
            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            while (csv.Read())
            {
                var record = new Dictionary<string, string>();
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        record[header] = csv.GetField(header) ?? string.Empty;
                    }
                }
                records.Add(record);
            }

            return records;
        }
    }
}
