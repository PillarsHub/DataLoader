using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using System.Globalization;

namespace DataLoader.Services.Import
{
    internal class CustomerImporter
    {
        private readonly CustomerRepository _customerRepository;
        private readonly CSVFileReader _csvFileReader;

        public CustomerImporter(CustomerRepository customerRepository, CSVFileReader sVFileReader)
        {
            _customerRepository = customerRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImpmortCustomers(string filePath)
        {
            var timeZoneId = "Pacific Standard Time";
            var dateFormat = "M/d/yyyy H:mm";

            var data = _csvFileReader.ReadCsvFile(filePath);

            List<object> ErrorList = new List<object>();

            foreach (var row in data)
            {
                
                //,
                //SecondaryPhone,
                //TextPhone,


                var signUpDate = ReadDate(row["SignUpDate"], dateFormat, timeZoneId);
                var birthDate = ReadDate(row["BirthDate"], dateFormat, timeZoneId);
                int.TryParse(row["CustomerType"], out int customerType);
                int.TryParse(row["StatusID"], out int status);

                var billAddress = new Address
                {
                    Type = "Billing",
                    Line1 = row["BillAddress1"],
                    Line2 = row["BillAddress2"],
                    City = row["BillCity"],
                    StateCode = row["BillState"],
                    Zip = row["BillPostalCode"],
                    CountryCode = row["BillCountry"]
                };

                var shipAddress = new Address
                {
                    Type = "Shipping",
                    Line1 = row["ShipAddress1"],
                    Line2 = row["ShipAddress2"],
                    City = row["ShipCity"],
                    StateCode = row["ShipState"],
                    Zip = row["ShipPostalCode"],
                    CountryCode = row["ShipCountry"]
                };

                var customer = new Customer
                {
                    Id = row["NodeID"],
                    FirstName = row["FirstName"],
                    LastName = row["LastName"],
                    CompanyName = row["CompanyName"],
                    EmailAddress = row["EmailAddress"],
                    Language = row["Language"],
                    WebAlias = row["WebAlias"],
                    BirthDate = birthDate,
                    SignupDate = signUpDate,
                    CustomerType = customerType,
                    Addresses = new List<Address>(new[] { billAddress, shipAddress }),
                    PhoneNumbers = new List<PhoneNumber>(new[] { new PhoneNumber { Number = row["PrimaryPhone"], Type = "primary" } })
                };

                try
                {
                    await _customerRepository.SaveCustomer(customer);

                    //await _customerRepository.InsertSource(new Sources.Source
                    //{
                    //    Date = signUpDate.Value,
                    //    ExternalId = Guid.NewGuid().ToString(),
                    //    NodeId = row["NodeID"],
                    //    SourceGroupId = "Status",
                    //    Value = status == 1 ? "ACT" : "INA"
                    //});
                }
                catch (Exception ex)
                {
                    ErrorList.Add(new { msg = ex.Message, cust = customer });
                }


                Console.WriteLine($"Imported {row["NodeID"]}");
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
