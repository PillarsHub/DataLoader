using DataLoader.Repositories;

namespace DataLoader.Services.Import
{
    internal class NodeImporter
    {
        private readonly TreeRepository _treeRepository;
        private readonly NodeRepository _nodeRepository;
        private readonly CSVFileReader _csvFileReader;

        public NodeImporter(TreeRepository treeRepository, NodeRepository nodeRepository, CSVFileReader sVFileReader)
        {
            _treeRepository = treeRepository;
            _nodeRepository = nodeRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImpmortNodes(string filePath)
        {
            //recordnumber,last_modified,DistributorID,UplineID,UplineLeg,TreeIndex,UplineIndex

            var trees = await _treeRepository.GetTrees();
            Console.WriteLine("Please select a tree to import into");

            foreach (var t in trees)
            {
                Console.WriteLine($"{t.Id} - {t.Name}");
            }

            var treeIdString = Console.ReadLine();
            long.TryParse(treeIdString, out long treeId);

            var data = _csvFileReader.ReadCsvFile(filePath);

            List<Node> nodes = new List<Node>();
            List<string> badIds = new List<string>();

            foreach (var row in data)
            {
                row.TryGetValue("nodeId", out string? nodeId);
                row.TryGetValue("uplineId", out string? uplineId);
                if (nodeId != null && uplineId != null)
                {
                    var leg = nodeId;

                    if (treeId > 0 && uplineId != null)
                    {
                        nodes.Add(new Node { NodeId = nodeId, UplineId = uplineId, UplineLeg = leg });
                        Console.WriteLine($"Imported {nodeId} {uplineId}");
                    }
                    else
                    {
                        badIds.Add(nodeId);
                    }
                }
            }

            List<object> ErrorList = new List<object>();

            foreach (var node in nodes)
            {
                try
                {
                    await _nodeRepository.InsertNode(treeId, node.NodeId, node.UplineId, node.UplineLeg);
                    Console.WriteLine($"Imported {node.NodeId} {node.UplineId}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new { msg = ex.Message, node = node });
                    }
                }
            }

            Console.WriteLine($"Imported {data.Count} rows");
        }
    }
}
