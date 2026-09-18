using DataLoader.Importer;
using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using DataLoader.TestData;
using Importer.Contracts;
using System.Collections.Concurrent;

namespace DataLoader.Services.Import
{
    internal class DataModelImporter
    {
        private readonly CSVFileReader _csvFileReader;
        private readonly TreeRepository _treeRepository;
        private readonly CustomerService _customerService;
        private readonly SourceGroupRepository _sourceGroupRepository;

        public DataModelImporter(CSVFileReader csvFileReader, TreeRepository treeRepository, CustomerService customerService, SourceGroupRepository sourceGroupRepository)
        {
            _csvFileReader = csvFileReader;
            _treeRepository = treeRepository;
            _customerService = customerService;
            _sourceGroupRepository = sourceGroupRepository;
        }

        public async Task Import(string filePath)
        {
            var allTrees = await _treeRepository.GetTrees();
            var allSourceGroups = await _sourceGroupRepository.GetSourceGroups();
            var customerTypegroup = allSourceGroups.FirstOrDefault(x => string.Equals(x.Id, "CustType", StringComparison.OrdinalIgnoreCase));
            if (customerTypegroup == null)
            {
                Console.WriteLine($"No Customer Types were found to map to.");
                return;
            }

            var data = _csvFileReader.ReadCsvFile(filePath);
            var keys = data[0].Keys.ToArray();

            var CustTypeKey = keys[0];
            var FirstNameKey = keys[1];
            var LastNameKey = keys[2];
            var IdKey = keys[3];
            var DownlineCountKey = keys[4];

            var treeKeys = FindTreeColumns(keys, allTrees, 5);
            var volumeKeys = FindVolumeKeys(keys, 5 + (treeKeys.Length * 2));

            Console.WriteLine();
            var treeNameArray = string.Join(",", treeKeys.Select(x => x.TreeName).ToArray());
            Console.WriteLine($"Imported Model contains {treeKeys.Length} trees. ({treeNameArray})");

            var volumeNameArray = string.Join(",", volumeKeys);
            Console.WriteLine($"Imported Model contains {volumeKeys.Length} volumes. ({volumeNameArray})");

            var model = new List<DataModelSpecification>();

            foreach (var row in data)
            {
                int.TryParse(row[DownlineCountKey], out int downlineCount);

                var customType = customerTypegroup.AcceptedValues?.FirstOrDefault(x => string.Equals(x.Description, row[CustTypeKey], StringComparison.OrdinalIgnoreCase));
                if (customType == null)
                {
                    Console.WriteLine($"No Customer Types were found to map to.");
                    return;
                }

                int.TryParse(customType.Value, out int customerTypeInt);

                var rowSpec = new DataModelSpecification
                {
                    CustomerType = customerTypeInt,
                    FirstName = row[FirstNameKey],
                    LastName = row[LastNameKey],
                    Id = row[IdKey],
                    DownlineCount = downlineCount,
                    UplineIds = treeKeys.Select(x => 
                    {
                        bool.TryParse(row[x.RandomDownlineKey], out bool randomDownline);
                        return new DataModelUplineIds
                        {
                            TreeId = x.TreeId,
                            RandomDownline = randomDownline,
                            UplineId = row[x.UplineIdKey]
                        };
                    }).ToArray(),
                    Volume = volumeKeys.Select(x => 
                    {
                        decimal.TryParse(row[x], out decimal volumeAmount);
                        return new DataModelVolume
                        {
                            VolumeId = x,
                            VolumeAmount = volumeAmount
                        };
                    }).ToArray()
                };

                model.Add(rowSpec);
            }


            Console.WriteLine();
            Console.WriteLine("Proceed with Model Import.");
            Console.Write("> ");
            var input = Console.ReadLine()?.ToLower() ?? string.Empty;

            var total = 0;
            if (input == "y")
            {
                var customerIds = new ConcurrentDictionary<string, Customer>();

                int row = 0;
                foreach (var item in model)
                {
                    total += Math.Max(item.DownlineCount, 1);
                    if (row != 0)
                    {
                        Console.WriteLine($"Createing {Math.Max(item.DownlineCount, 1)} customers for model row {row}/{model.Count - 1})");
                        await ImportModelItem(item, customerIds);
                    }
                    row += 1;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Model imported {total} customers successfully.");
            
        }

        private async Task ImportModelItem(DataModelSpecification item, ConcurrentDictionary<string, Customer> customerIds)
        {
            if (item.DownlineCount > 0)
            {
                for (int i = 0; i < item.DownlineCount; i++)
                {
                    await CreateCustomer(new DataModelSpecification
                    {
                        CustomerType = item.CustomerType,
                        UplineIds = item.UplineIds,
                        Volume = item.Volume,
                        DownlineCount = 0,
                        FirstName = null,
                        LastName = null,
                        Id = null
                    }, customerIds);
                }
            }
            else
            {
                await CreateCustomer(item, customerIds);
            }
        }

        private async Task CreateCustomer(DataModelSpecification item, ConcurrentDictionary<string, Customer> customerIds)
        {
            var volumes = item.Volume.Select(x => (x.VolumeId, x.VolumeAmount)).ToList();
            var uplines = item.UplineIds.Select(x => (x.TreeId, x.UplineId)).ToList();

            await _customerService.CreateCustomer(item.Id, item.FirstName, item.LastName, item.CustomerType, uplines, volumes, customerIds);
        }

        private string[] FindVolumeKeys(string[] keys, int index)
        {
            var results = new List<string>();
            for (int i = index; i < keys.Length; i++)
            {
                results.Add(keys[i]);
            }

            return results.ToArray();
        }

        private TreeColumnKey[] FindTreeColumns(string[] keys, Tree[] trees, int index)
        {
            var results = new List<TreeColumnKey>();
            var nextPossibleTree = keys[index];
            var treeName = nextPossibleTree.Replace("Upline", "").Trim();

            foreach (var tree in trees)
            {
                if (string.Equals(tree.Name, treeName, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new TreeColumnKey
                    {
                        TreeId = tree.Id,
                        TreeName = tree.Name,
                        UplineIdKey = keys[index],
                        RandomDownlineKey = keys[index + 1],
                    });

                    results.AddRange(FindTreeColumns(keys, trees, index + 2));
                    break;
                }
            }

            return results.ToArray();
        }
    }

    internal class TreeColumnKey
    {
        public string UplineIdKey { get; set; } = string.Empty;
        public string RandomDownlineKey { get; set; } = string.Empty;
        public long TreeId { get; set; }
        public string TreeName { get; set; } = string.Empty;
    }

    internal class DataModelSpecification
    {
        public int CustomerType { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; } 
        public string? Id { get; set; } 
        public int DownlineCount { get; set; }
        public DataModelUplineIds[] UplineIds { get; set; } = Array.Empty<DataModelUplineIds>();
        public DataModelVolume[] Volume { get; set; } = Array.Empty<DataModelVolume>();
    }

    internal class DataModelUplineIds
    {
        public long TreeId { get; set; }
        public string UplineId { get; set; } = string.Empty;
        public bool RandomDownline { get; set; }
    }

    internal class DataModelVolume
    {
        public string VolumeId { get; set; } = string.Empty;
        public decimal VolumeAmount { get; set; }
    }
}
