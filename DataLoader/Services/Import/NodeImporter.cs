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

        private async Task<Node?> GetNode(string nodeId)
        {
            try //Try Binary Tree
            {
                return await _nodeRepository.GetNode(215, nodeId);
            }
            catch
            {
                try //Tree Placement Tree
                {
                    return await _nodeRepository.GetNode(214, nodeId);
                }
                catch
                {
                    return null;
                }
            }
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

            //var treeIdString = Console.ReadLine();
            //long.TryParse(treeIdString, out long treeId);
            var treeId = 215;

            var data = _csvFileReader.ReadCsvFile(filePath);

            List<Node> nodes = new List<Node>();
            List<string> badIds = new List<string>();

            foreach (var row in data)
            {
                row.TryGetValue("AssociateID", out string? nodeId);
                row.TryGetValue("EnrollerID", out string? uplineId);
                if (nodeId != null && uplineId != null)
                {
                    var leg = "Holding Tank";

                    if (treeId > 0 && uplineId != null)
                    {
                        //await _nodeRepository.InsertNode(treeId, nodeId, uplineId, leg);
                        nodes.Add(new Node { NodeId = nodeId, UplineId = uplineId, UplineLeg = leg });
                        Console.WriteLine($"Imported {nodeId} {uplineId}");
                    }
                    else
                    {
                        badIds.Add(nodeId);
                    }
                }
            }

            foreach(var node in nodes)
            {
                await _nodeRepository.InsertNode(treeId, node.NodeId, node.UplineId, node.UplineLeg);
                Console.WriteLine($"Imported {node.NodeId} {node.UplineId}");
            }

            Console.WriteLine($"Imported {data.Count} rows");
        }
    }
}
