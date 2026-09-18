using DataLoader.Importer;
using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using Importer.Contracts;

namespace DataLoader.Services.Import;

internal class CustomerImporter
{
    private readonly CustomerRepository _customerRepository;
    private readonly SourceGroupRepository _sourceGroupRepository;
    private readonly CSVFileReader _csvFileReader;

    public CustomerImporter(CustomerRepository customerRepository, CSVFileReader sVFileReader, SourceGroupRepository sourceGroupRepository)
    {
        _customerRepository = customerRepository;
        _csvFileReader = sVFileReader;
        _sourceGroupRepository = sourceGroupRepository;
    }

    public async Task ImpmortCustomers(string basePath, IImportProfile<CustomersImportRequest>? customerProfile)
    {
        if (customerProfile == null)
        {
            Console.WriteLine($"No customer map to import.");
            return;
        }

        Console.WriteLine($"Reading Customer Headers");
        var data = _csvFileReader.ReadCsvFile(Path.Combine(basePath, customerProfile.SourceFile));

        List<Customer> customers = new List<Customer>();

        foreach (var row in data)
        {
            var customerHeader = customerProfile.Map(row);
            if (customerHeader == null)
            {
                Console.WriteLine($"Skipping row with missing customer header data: {string.Join(", ", row.Values)}");
                continue;
            }

            var billAddress = new Address
            {
                Type = "Billing",
                Line1 = customerHeader.BillLine1 ?? "",
                Line2 = customerHeader.BillLine2 ?? "",
                City = customerHeader.BillCity ?? "",
                StateCode = customerHeader.BillState ?? "",
                Zip = customerHeader.BillZip ?? "",
                CountryCode = customerHeader.BillCountry ?? ""
            };

            var shipAddress = new Address
            {
                Type = "Shipping",
                Line1 = customerHeader.ShipLine1 ?? "",
                Line2 = customerHeader.ShipLine2 ?? "",
                City = customerHeader.ShipCity ?? "",
                StateCode = customerHeader.ShipState ?? "",
                Zip = customerHeader.ShipZip ?? "",
                CountryCode = customerHeader.ShipCountry ?? ""
            };

            var phoneNumbers = new List<PhoneNumber>();
            if (!string.IsNullOrWhiteSpace(customerHeader.PrimaryPhone))
            {
                phoneNumbers.Add(new PhoneNumber { Number = customerHeader.PrimaryPhone, Type = "primary" });
            }

            if (!string.IsNullOrWhiteSpace(customerHeader.SecondaryPhone))
            {
                phoneNumbers.Add(new PhoneNumber { Number = customerHeader.SecondaryPhone, Type = "secondary" });
            }

            if (!string.IsNullOrWhiteSpace(customerHeader.TextPhone))
            {
                phoneNumbers.Add(new PhoneNumber { Number = customerHeader.TextPhone, Type = "text" });
            }


            customers.Add(new Customer
            {
                Id = customerHeader.CustomerId,
                ExternalIds = string.IsNullOrWhiteSpace(customerHeader.ExternalId) ? [] : [customerHeader.ExternalId],
                FirstName = customerHeader.FirstName,
                LastName = customerHeader.LastName,
                CompanyName = customerHeader.CompanyName,
                EmailAddress = customerHeader.EmailAddress,
                Language = customerHeader.Language,
                WebAlias = !string.IsNullOrWhiteSpace(customerHeader.WebAlias) ? customerHeader.WebAlias : null,
                Status = customerHeader.Status,
                BirthDate = customerHeader.BirthDate,
                SignupDate = customerHeader.SignupDate,
                CustomerType = customerHeader.CustomerType,
                Addresses = [billAddress, shipAddress],
                PhoneNumbers = phoneNumbers
            });
        }

        List<ErrorItem> ErrorList = new List<ErrorItem>();

        var sg = await _sourceGroupRepository.GetSourceGroups();
        var custTypes = sg.FirstOrDefault(x => x.Id == "CustType")?.AcceptedValues?.Select(x => x.Value).ToHashSet();
        var missingCustomerType = customers.Where(x => !custTypes?.Contains(x.CustomerType.ToString()) ?? true).Select(x => x.CustomerType).Distinct();

        if (missingCustomerType.Any())
        {
            Console.WriteLine($"Customer TYpe not found: {string.Join(",", missingCustomerType)}");
        }
        else
        {
            MakeEmailsUnique(customers);
            MakeWebAliasesUnique(customers);

            await Parallel.ForEachAsync(customers, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (customer, cancellationToken) =>
            {
                try
                {
                    await _customerRepository.SaveCustomer(customer, !string.IsNullOrWhiteSpace(customer.Id));
                    Console.WriteLine($"Imported {customer.Id}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new ErrorItem { Message = ex.Message, Items = [customer] });
                    }
                }
            });

            var errors = ErrorList.GroupBy(x => x.Message).ToDictionary(x => x.Key, x => x.ToList());

            foreach (var error in errors)
            {
                Console.WriteLine($"Error during import: {error.Key} count: {error.Value.Count}");
            }

            Console.WriteLine($"Imported {data.Count} rows");
        }
    }

    private void MakeEmailsUnique(List<Customer> customers)
    {
        var usedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var customer in customers)
        {
            if (string.IsNullOrWhiteSpace(customer.EmailAddress))
                continue;

            var originalEmail = customer.EmailAddress.Trim();
            if (usedEmails.Add(originalEmail)) continue;

            customer.EmailAddress = GetNextUniqueEmail(originalEmail, usedEmails);
            usedEmails.Add(customer.EmailAddress);
        }
    }

    private void MakeWebAliasesUnique(List<Customer> customers)
    {
        var usedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var customer in customers)
        {
            if (string.IsNullOrWhiteSpace(customer.WebAlias))
                continue;

            var originalAlias = customer.WebAlias.Trim();
            if (usedAliases.Add(originalAlias)) continue;

            customer.WebAlias = GetNextUniqueWebAlias(originalAlias, usedAliases);
            usedAliases.Add(customer.WebAlias);
        }
    }

    private string GetNextUniqueEmail(string email, HashSet<string> usedEmails)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
        {
            // Fallback if email is malformed
            int fallbackCounter = 1;
            string fallbackCandidate;
            do
            {
                fallbackCandidate = $"{email}+{fallbackCounter}";
                fallbackCounter++;
            }
            while (usedEmails.Contains(fallbackCandidate));

            return fallbackCandidate;
        }

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..];

        int counter = 1;
        string candidate;

        do
        {
            candidate = $"{localPart}+{counter}{domainPart}";
            counter++;
        }
        while (usedEmails.Contains(candidate));

        return candidate;
    }

    static string GetNextUniqueWebAlias(string webAlias, HashSet<string> usedAliases)
    {
        int counter = 1;
        string candidate;

        do
        {
            candidate = $"{webAlias}_{counter}";
            counter++;
        }
        while (usedAliases.Contains(candidate));

        return candidate;
    }

    private class ErrorItem
    {
        public string Message { get; set; } = "";
        public Customer[] Items { get; set; } = [];
    }
}
