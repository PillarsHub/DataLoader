using DataLoader.Http;
using DataLoader.Repositories;

namespace DataLoader.Services.Import
{
    internal class ExtendedImporter
    {
        private readonly NodeService _nodeService;
        private readonly CSVFileReader _csvFileReader;

        public ExtendedImporter(CSVFileReader csvFileReader, NodeService nodeService)
        {
            _csvFileReader = csvFileReader;
            _nodeService = nodeService;
        }

        public async Task Import(string csvFile)
        {
            var data = _csvFileReader.ReadCsvFile(csvFile);
            var treeId = 1161L;

            var verifiedList = new List<(string, string)>();

            foreach (var row in data)
            {
                var nodeId = row["ID"];
                var uplineId = row["Upline"];

                try
                {
                    var currentNode = await _nodeService.GetNode(treeId, nodeId, null);

                    if (currentNode == null || currentNode.UplineId != uplineId)
                    {
                        throw new Exception("Node not under root node");
                    }

                    verifiedList.Add((nodeId, uplineId));
                }
                catch (NotFoundException)
                {
                    Console.WriteLine($"Node {nodeId} not found. Skipping Node.");
                }

                Console.WriteLine($"Verified {nodeId}");
            }

            foreach(var item in verifiedList)
            {
                var newUplineId = await _nodeService.FindRandomDownline(treeId, item.Item1, item.Item2, 10);
                if (!string.IsNullOrWhiteSpace(newUplineId))
                {
                    await _nodeService.InsertNode(treeId, item.Item1, newUplineId, item.Item1, null);
                }

                Console.WriteLine($"Imported {item.Item1} to {newUplineId}");
            }

            Console.WriteLine($"Imported {data.Count} rows");
        }
    }
}
