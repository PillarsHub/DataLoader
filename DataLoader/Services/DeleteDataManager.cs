using DataLoader.Repositories;

namespace DataLoader.Services
{
    internal class DeleteDataManager
    {
        private readonly CustomerRepository _customerRepository;
        private readonly CSVFileReader _csvFileReader;

        public DeleteDataManager(CustomerRepository customerRepository, CSVFileReader sVFileReader)
        {
            _customerRepository = customerRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task BeginDelete()
        {
            Console.WriteLine("What would you like to Delete");
            Console.WriteLine("(C)ustomers");

            Console.Write("> ");
            var input = Console.ReadLine()?.ToLower() ?? string.Empty;

            if (input == "c")
            {
                string? customerIdPath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(customerIdPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await DeleteCustomers(customerIdPath.Trim('"'));
            }
        }

        private async Task DeleteCustomers(string customerIdPath)
        {
            Console.WriteLine($"Reading Customer ids");
            var data = _csvFileReader.ReadCsvFile(customerIdPath);

            var IdsToDelete = new List<string>();
            foreach (var row in data)
            {
                IdsToDelete.Add(row["CustomerId"]);
            }

            var aa = 0;

            var count = 0;
            foreach (var id in IdsToDelete)
            {
                count++;
                await _customerRepository.DeleteCustomer(id);
                Console.WriteLine($"Deleteting Customer {id} - {count}/{IdsToDelete.Count()}");
            }
        }


        private string? GetFilePathFromDialog()
        {
            Console.WriteLine($"Enter path to .csv file");
            return Console.ReadLine()?.Trim('"');
        }
    }
}
