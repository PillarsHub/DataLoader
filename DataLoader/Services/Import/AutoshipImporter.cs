using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using System.Globalization;

namespace DataLoader.Services.Import
{
    internal class AutoshipImporter
    {
        private readonly AutoshipRepository _repository;
        private readonly CSVFileReader _csvFileReader;

        public AutoshipImporter(CSVFileReader sVFileReader, AutoshipRepository repository)
        {
            _repository = repository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImportAutoships(string orderFile, string lineItemFile)
        {
            var timeZoneId = "Mountain Standard Time";
            var dateFormat = "M/d/yyyy H:mm";

            await Task.CompletedTask;
            var autoshipRows = _csvFileReader.ReadCsvFile(orderFile);
            var lineItemRows = _csvFileReader.ReadCsvFile(lineItemFile);

            var lineItems = lineItemRows.Select(x =>
            {
                decimal.TryParse(x["quantity"], out decimal quantity);

                return new AutoshipLineItem
                {
                    AutoshipId = x["autoshipId"],
                    ItemId = x["itemId"],
                    Quantity = quantity,

                };
            }).GroupBy(x => x.AutoshipId).ToDictionary(x => x.Key, y => y.ToArray());

            var autoships = autoshipRows.Select(x =>
            {
                var autoshipId = x["id"];
                var startDate = ReadDate(x["startDate"], dateFormat, timeZoneId);

                if (!Enum.TryParse(x["frequency"], true, out Frequency frequency))
                {
                    frequency = Frequency.Monthly;
                }

                //lastAutoshipDate
                //nextAutoshipDate

                lineItems.TryGetValue(autoshipId, out AutoshipLineItem[]? items);
                if (!startDate.HasValue) throw new ArgumentNullException(nameof(startDate));

                return new Autoship
                {
                    Id = autoshipId,
                    CustomerId = x["customerId"],
                    StartDate = startDate.Value,
                    Frequency = frequency,
                    AutoshipType = x["autoshipType"],
                    ShippingMethod = x["shippingMethod"],
                    PaymentMethod = x["paymentMethod"],
                    Status = x["status"],

                    Address = new ShipAddress
                    {
                        Line1 = x["line1"],
                        Line2 = x["line2"],
                        City = x["city"],
                        StateCode = x["stateCode"],
                        Zip = x["zip"],
                        CountryCode = x["countryCode"],
                    },

                    LineItems = items != null ? items : Array.Empty<AutoshipLineItem>(),
                };
            }).ToArray();

            List<object> ErrorList = new List<object>();

            //await Parallel.ForEachAsync(autoships, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (autoship, cancellationToken) =>
            foreach(var autoship in autoships)
            {
                try
                {
                    await _repository.InsertAutoship(autoship);
                    Console.WriteLine($"Imported {autoship.Id} {autoship.CustomerId}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new { msg = ex.Message, autoship = autoship });
                    }
                }
            }


            string aa = string.Empty;
            foreach (var item in ErrorList)
            {
                aa += "\r\n" + item;
            }

            Console.WriteLine($"Imported {autoships.Length} rows");
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
