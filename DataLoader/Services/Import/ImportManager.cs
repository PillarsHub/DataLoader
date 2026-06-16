using DataLoader.Repositories;
using Importer.Contracts;

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
        private readonly TreeRepository _treeRepository;

        public ImportManager(NodeImporter nodeImporter, OrderImporter orderImporter, CustomerImporter customerImporter, InventoryImporter inventoryImporter,
            HistoricalValueImporter historicalValueImporter, SourceImporter sourceImporter, HistoricalBonusImporter historicalBonusImporter, 
            AutoshipImporter autoshipImporter, PaymentTokenImporter paymentTokenImporter, TreeRepository treeRepository)
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
            _treeRepository = treeRepository;
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
                if (GetCsxFilePathFromDialog(out string dirPath, out string csxPath))
                {
                    Console.WriteLine("What tree are you importing?");
                    Console.WriteLine(" (E)nrollment Tree");
                    Console.WriteLine(" (P)lacement Tree");
                    Console.WriteLine(" (B)inary Tree");
                    var treeTypeString = Console.ReadLine()?.ToLower() ?? string.Empty; ;

                    var trees = await _treeRepository.GetTrees();
                    Console.WriteLine("Please select a tree to import into");

                    foreach (var t in trees)
                    {
                        Console.WriteLine($"{t.Id} - {t.Name}");
                    }

                    var treeIdString = Console.ReadLine();
                    long.TryParse(treeIdString, out long treeId);
                    
                    if (treeTypeString == "e")
                    {
                        var headerProfile = await ScriptLoader.LoadProfileAsync<EnrollmentTreeImportRequest>(csxPath);
                        await _nodeImporter.ImportEnrollmentNodes(dirPath, treeId, headerProfile);
                    }
                    
                    if (treeTypeString == "p")
                    {
                        var headerProfile = await ScriptLoader.LoadProfileAsync<PlacementTreeImportRequest>(csxPath);
                        await _nodeImporter.ImpmortPlacementNodes(dirPath, treeId, headerProfile);
                    }

                    if (treeTypeString == "b")
                    {
                        var headerProfile = await ScriptLoader.LoadProfileAsync<BinaryTreeImportRequest>(csxPath);
                        await _nodeImporter.ImpmortBinaryNodes(dirPath, treeId, headerProfile);
                    }
                }
            }
            else if (input == "a")
            {
                string? orderPath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(orderPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }
                string? lineItemPath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(lineItemPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }


                await _autoshipImporter.ImportAutoships(orderPath.Trim('"'), lineItemPath.Trim('"'));
            }
            else if (input == "o")
            {
                if (GetCsxFilePathFromDialog(out string dirPath, out string csxPath))
                {
                    var headerProfile = await ScriptLoader.LoadProfileAsync<OrderHeaderImportRequest>(csxPath);
                    var lineItemProfile = await ScriptLoader.LoadProfileAsync<OrderLineItemImportRequest>(csxPath);
                    var volumeProfile = await ScriptLoader.LoadProfileAsync<OrderVolumeImportRequest>(csxPath);
                    var paymentsProfile = await ScriptLoader.LoadProfileAsync<OrderPaymentImportRequest>(csxPath);
                    await _orderImporter.ImportOrders(dirPath, headerProfile, lineItemProfile, volumeProfile, paymentsProfile);
                }
            }
            else if (input == "c")
            {
                if (GetCsxFilePathFromDialog(out string dirPath, out string csxPath))
                {
                    var customerProfile = await ScriptLoader.LoadProfileAsync<CustomersImportRequest>(csxPath);
                    await _customerImporter.ImpmortCustomers(dirPath, customerProfile);
                }
            }
            else if (input == "t")
            {
                string? customerPath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(customerPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _paymentTokenImporter.ImportPaymentTokens(customerPath.Trim('"'));
            }
            else if (input == "v")
            {
                string? customerPath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(customerPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _historicalValueImporter.Import(customerPath.Trim('"'));
            }
            else if (input == "b")
            {
                string? customerPath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(customerPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _historicalBonusImporter.Import(customerPath.Trim('"'));
            }
            else if (input == "i")
            {
                string? productPath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(productPath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }
                string? pricePath = GetFilePathFromDialog();
                if (string.IsNullOrWhiteSpace(pricePath))
                {
                    Console.WriteLine("No file selected.");
                    return;
                }

                await _inventoryImporter.ImportInventory(productPath.Trim('"'), pricePath.Trim('"'));
            }
            else if (input == "s")
            {
                string? sourcePath = GetFilePathFromDialog();
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

        private static string? GetFilePathFromDialog()
        {
            Console.WriteLine($"Enter path to .csx file");
            return Console.ReadLine()?.Trim('"');
        }

        private static bool GetCsxFilePathFromDialog(out string dirPath, out string csxPath)
        {
            Console.WriteLine($"Enter path to .csx file");
            var path = Console.ReadLine()?.Trim('"');

            dirPath = string.Empty;
            csxPath = string.Empty;

            if (!File.Exists(path))
            {
                Console.WriteLine("No file selected.");
                return false;
            }

            var dirP = Path.GetDirectoryName(path);
            if (dirP == null || !Directory.Exists(dirP))
            {
                Console.WriteLine("No file selected.");
                return false;
            }

            dirPath = dirP;
            csxPath = path;
            return true;
        }

    }
}
