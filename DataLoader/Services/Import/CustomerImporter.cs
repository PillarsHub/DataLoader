using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using System.Collections.Concurrent;
using System.Globalization;

namespace DataLoader.Services.Import
{
    internal class CustomerImporter
    {
        private readonly CustomerRepository _customerRepository;
        private readonly CSVFileReader _csvFileReader;
        private readonly ConcurrentDictionary<string, int> _emailTracker = new();

        public CustomerImporter(CustomerRepository customerRepository, CSVFileReader sVFileReader)
        {
            _customerRepository = customerRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImpmortCustomers(string filePath)
        {
            var timeZoneId = "Mountain Standard Time";
            var dateFormat = "M/d/yyyy H:mm";

            var data = _csvFileReader.ReadCsvFile(filePath);

            List<Customer> customers = new List<Customer>();

            foreach (var row in data)
            {
                var signUpDate = ReadDate(row["signupDate"], dateFormat, timeZoneId);
                var birthDate = ReadDate(row["birthDate"], dateFormat, timeZoneId);
                int.TryParse(row["customerType"], out int customerType);
                int.TryParse(row["status"], out int status);

                var billAddress = new Address
                {
                    Type = "Billing",
                    Line1 = row["billline1"],
                    Line2 = row["billline2"],
                    City = row["billcity"],
                    StateCode = row["billstateCode"],
                    Zip = row["billzip"],
                    CountryCode = row["billcountryCode"]
                };

                var shipAddress = new Address
                {
                    Type = "Shipping",
                    Line1 = row["shipline1"],
                    Line2 = row["shipline2"],
                    City = row["shipcity"],
                    StateCode = row["shipstateCode"],
                    Zip = row["shipzip"],
                    CountryCode = row["shipcountryCode"]
                };

                var customerId = row["id"];
                var email = row["emailAddress"];

                //if (status == 5)
                //{
                //    email = AppendValueToEmail(email, "T");
                //    status = 4;
                //}

                customers.Add(new Customer
                {
                    Id = customerId,
                    FirstName = row["firstName"],
                    LastName = row["lastName"],
                    CompanyName = row["companyName"],
                    EmailAddress = email,
                    Language = row["language"],
                    WebAlias = row["webAlias"],
                    Status = (status * 10).ToString(),
                    BirthDate = birthDate,
                    SignupDate = signUpDate,
                    CustomerType = customerType,
                    Addresses = new List<Address>(new[] { billAddress, shipAddress }),
                    PhoneNumbers = new List<PhoneNumber>(new[] { new PhoneNumber { Number = row["primaryPhone"], Type = "primary" } })
                });
            }

            List<ErrorItem> ErrorList = new List<ErrorItem>();

            var duplicateEmails = customers.GroupBy(x => x.EmailAddress).Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Count() > 1);

            await Parallel.ForEachAsync(customers, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (customer, cancellationToken) =>
            {
                try
                {
                    await _customerRepository.SaveCustomer(customer);
                }
                catch (Exception ex)
                {
                    ErrorList.Add(new ErrorItem { Message = ex.Message, Item = customer });
                }

                Console.WriteLine($"Imported {customer.Id}");
            });


            var errors = ErrorList.GroupBy(x => x.Message).ToDictionary(x=> x.Key, x => x.ToList());

            Console.WriteLine($"Imported {data.Count} rows");
        }

        //public string AppendValueToEmail(string email, string value)
        //{
        //    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(value)) return string.Empty;

        //    int count = _emailTracker.AddOrUpdate(email, 1, (_, existingCount) => existingCount + 1);
        //    string indexPart = count > 1 ? count.ToString() : string.Empty;

        //    var parts = email.Split('@');
        //    if (parts.Length != 2)
        //        return $"{email}+{value}{indexPart}";

        //    return $"{parts[0]}+{value}{indexPart}@{parts[1]}";
        //}

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


    internal class EmailMap
    {
        public string CustomerId { get; set; }
        public string EmailAddress { get; set; }
    }

    internal class ErrorItem
    {
        public string Message { get; set; }
        public object Item { get; set; }
    }
}
