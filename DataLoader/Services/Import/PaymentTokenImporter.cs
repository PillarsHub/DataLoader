using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using System.Globalization;

namespace DataLoader.Services.Import
{
    internal class PaymentTokenImporter
    {
        private readonly PaymentTokenRepository _repository;
        private readonly CSVFileReader _csvFileReader;

        public PaymentTokenImporter(CSVFileReader sVFileReader, PaymentTokenRepository repository)
        {
            _repository = repository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImportPaymentTokens(string tokenFile)
        {
            var timeZoneId = "Mountain Standard Time";
            var dateFormat = "M/d/yyyy H:mm";

            await Task.CompletedTask;
            var tokenRows = _csvFileReader.ReadCsvFile(tokenFile);

            var tokens = tokenRows.Select(x =>
            {
                var expirationDate = ReadDate(x["expirationDate"], dateFormat, timeZoneId);

                return new PaymentToken
                {
                    CustomerId = x["ID"],
                    MerchantId = x["MerchantID"],
                    ExpirationDate = expirationDate,
                    Input1 = x["input1"],
                    //Input2 = x["customerId"],
                    Input3 = x["Input3"],
                    Input4 = x["Input4"],
                    Last4CC = x["Last4CC"],
                    Name = x["PaymentMethodName"],
                    ProfileType = x["profileType"],
                    Token = x["CustomerVaultIDToken"],

                    Address = new ShipAddress
                    {
                        Line1 = x["Address1"],
                        Line2 = x["Address2"],
                        City = x["City"],
                        StateCode = x["State"],
                        Zip = x["Zip"],
                        CountryCode = x["CountryCode"],
                    },
                };
            }).ToArray();

            List<object> ErrorList = new List<object>();

            foreach (var token in tokens)
            {
                try
                {
                    await _repository.InsertToken(token);
                    Console.WriteLine($"Imported {token.Id} {token.CustomerId}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new { msg = ex.Message, token = token });
                    }
                }
            }


            string aa = string.Empty;
            foreach (var item in ErrorList)
            {
                aa += "\r\n" + item;
            }

            Console.WriteLine($"Imported {tokens.Length} rows");
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
