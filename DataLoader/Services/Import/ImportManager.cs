namespace DataLoader.Services.Import
{
    internal class ImportManager
    {
        private readonly NodeImporter _nodeImporter;
        private readonly OrderImporter _orderImporter;
        private readonly InventoryImporter _inventoryImporter;
        private readonly CustomerImporter _customerImporter;
        private readonly HistoricalValueImporter _historicalValueImporter;
        private readonly HistoricalBonusImporter _historicalBonusImporter;
        private readonly SourceImporter _sourceImporter;
        private readonly AutoshipImporter _autoshipImporter;
        private readonly PaymentTokenImporter _paymentTokenImporter;

        public ImportManager(NodeImporter nodeImporter, OrderImporter orderImporter, CustomerImporter customerImporter, InventoryImporter inventoryImporter,
            HistoricalValueImporter historicalValueImporter, SourceImporter sourceImporter, HistoricalBonusImporter historicalBonusImporter, 
            AutoshipImporter autoshipImporter, PaymentTokenImporter paymentTokenImporter)
        {
            _nodeImporter = nodeImporter;
            _orderImporter = orderImporter;
            _customerImporter = customerImporter;
            _historicalValueImporter = historicalValueImporter;
            _sourceImporter = sourceImporter;
            _inventoryImporter = inventoryImporter;
            _historicalBonusImporter = historicalBonusImporter;
            _autoshipImporter = autoshipImporter;
            _paymentTokenImporter = paymentTokenImporter;
        }

        public async Task BeginImport()
        {
            Console.WriteLine("What would you like to import");
            Console.WriteLine("(A)utoships");
            Console.WriteLine("(C)ustomers");
            Console.WriteLine("(N)odes");
            Console.WriteLine("(O)rders");
            Console.WriteLine("(S)ources");
            Console.WriteLine("(I)nventory");
            Console.WriteLine("Payment (T)okens");
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
            else if (input == "a")
            {
                string? orderPath = GetFilePathFromDialog("Autoships");
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


                await _autoshipImporter.ImportAutoships(orderPath.Trim('"'), lineItemPath.Trim('"'));
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


                await _orderImporter.ImportOrders(orderPath.Trim('"'), lineItemPath.Trim('"'));
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
            else if (input == "t")
            {
                string? customerPath = GetFilePathFromDialog("Payment Tokens");
                if (string.IsNullOrWhiteSpace(customerPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _paymentTokenImporter.ImportPaymentTokens(customerPath.Trim('"'));
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
            else if (input == "i")
            {
                string? productPath = GetFilePathFromDialog("Products");
                if (string.IsNullOrWhiteSpace(productPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }
                string? pricePath = GetFilePathFromDialog("Prices");
                if (string.IsNullOrWhiteSpace(pricePath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _inventoryImporter.ImportInventory(productPath.Trim('"'), pricePath.Trim('"'));
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
