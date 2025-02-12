namespace DataLoader.Services.Import
{
    internal class ImportManager
    {
        private readonly NodeImporter _nodeImporter;
        private readonly OrderImporter _orderImporter;
        private readonly CustomerImporter _customerImporter;
        private readonly HistoricalValueImporter _historicalValueImporter;
        private readonly HistoricalBonusImporter _historicalBonusImporter;
        private readonly SourceImporter _sourceImporter;

        public ImportManager(NodeImporter nodeImporter, OrderImporter orderImporter, CustomerImporter customerImporter, 
            HistoricalValueImporter historicalValueImporter, SourceImporter sourceImporter, HistoricalBonusImporter historicalBonusImporter)
        {
            _nodeImporter = nodeImporter;
            _orderImporter = orderImporter;
            _customerImporter = customerImporter;
            _historicalValueImporter = historicalValueImporter;
            _sourceImporter = sourceImporter;
            _historicalBonusImporter = historicalBonusImporter;
        }

        public async Task BeginImport()
        {
            Console.WriteLine("What would you like to import");
            Console.WriteLine("(N)odes");
            Console.WriteLine("(O)rders");
            Console.WriteLine("(C)ustomers");
            Console.WriteLine("(S)ources");
            Console.WriteLine("Historical (V)alues");
            Console.WriteLine("Historical (B)onuses");

            Console.Write("> ");
            var input = Console.ReadLine()?.ToLower() ?? string.Empty;

            if (input == "n")
            {
                string? filePath = GetFilePathFromDialog("Node");
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _nodeImporter.ImpmortNodes(filePath.Trim('"'));
            }
            else if (input == "o")
            {
                string? orderPath = GetFilePathFromDialog("Orders");
                if (string.IsNullOrWhiteSpace(orderPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }
                string? lineItemPath = GetFilePathFromDialog("Line Items");
                if (string.IsNullOrWhiteSpace(lineItemPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }


                await _orderImporter.ImpmortOrders(orderPath.Trim('"'), lineItemPath.Trim('"'));
            }
            else if (input == "c")
            {
                string? customerPath = GetFilePathFromDialog("Customers");
                if (string.IsNullOrWhiteSpace(customerPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _customerImporter.ImpmortCustomers(customerPath.Trim('"'));
            }
            else if (input == "v")
            {
                string? customerPath = GetFilePathFromDialog("Historical Values");
                if (string.IsNullOrWhiteSpace(customerPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _historicalValueImporter.Import(customerPath.Trim('"'));
            }
            else if (input == "b")
            {
                string? customerPath = GetFilePathFromDialog("Historical Bonues");
                if (string.IsNullOrWhiteSpace(customerPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _historicalBonusImporter.Import(customerPath.Trim('"'));
            }
            else if (input == "s")
            {
                string? sourcePath = GetFilePathFromDialog("Sources");
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _sourceImporter.Import(sourcePath.Trim('"'));
            }
            else
            {
                Console.WriteLine($"{input} is not a valid option");
            }
        }

        private static string? GetFilePathFromDialog(string file)
        {
            Console.WriteLine($"Enter path to CSV file for {file}");
            return Console.ReadLine();
        }
    }
}
