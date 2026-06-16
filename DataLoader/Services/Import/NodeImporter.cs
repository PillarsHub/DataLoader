using DataLoader.Importer;
using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using DataLoader.TestData;
using Importer.Contracts;

namespace DataLoader.Services.Import
{
    internal class NodeImporter
    {
        private readonly NodeRepository _nodeRepository;
        private readonly CSVFileReader _csvFileReader;

        public NodeImporter(NodeRepository nodeRepository, CSVFileReader sVFileReader)
        {
            _nodeRepository = nodeRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImportEnrollmentNodes(string basePath, long treeId, IImportProfile<EnrollmentTreeImportRequest>? nodeProfile)
        {
            if (nodeProfile == null) 
            {
                Console.WriteLine($"No node map to import.");
                return;
            }

            await ImportNodes(basePath, nodeProfile.SourceFile, treeId, row =>
            {
                var map = nodeProfile.Map(row);
                if (map == null) return null;

                return new Node
                {
                    NodeId = map.NodeId,
                    UplineId = map.UplineId,
                    UplineLeg = map.NodeId,
                    EffectiveDate = map.EffectiveDate
                };
            });
        }

        public async Task ImpmortPlacementNodes(string basePath, long treeId, IImportProfile<PlacementTreeImportRequest>? nodeProfile)
        {
            if (nodeProfile == null)
            {
                Console.WriteLine($"No node map to import.");
                return;
            }

            await ImportNodes(basePath, nodeProfile.SourceFile, treeId, row =>
            {
                var map = nodeProfile.Map(row);
                if (map == null) return null;

                return new Node
                {
                    NodeId = map.NodeId,
                    UplineId = map.UplineId,
                    UplineLeg = map.NodeId,
                    EffectiveDate = map.EffectiveDate
                };
            });
        }

        public async Task ImpmortBinaryNodes(string basePath, long treeId, IImportProfile<BinaryTreeImportRequest>? nodeProfile)
        {
            if (nodeProfile == null)
            {
                Console.WriteLine($"No node map to import.");
                return;
            }

            //Move all nodes to holding tank, to allow for proper placemens
            await ImportNodes(basePath, nodeProfile.SourceFile, treeId, row =>
            {
                var map = nodeProfile.Map(row);
                if (map == null) return null;

                return new Node
                {
                    NodeId = map.NodeId,
                    UplineId = map.UplineId,
                    UplineLeg = "Holding Tank",
                    EffectiveDate = map.EffectiveDate
                };
            });

            //Final Pass
            await ImportNodes(basePath, nodeProfile.SourceFile, treeId, row =>
            {
                var map = nodeProfile.Map(row);
                if (map == null) return null;

                return new Node
                {
                    NodeId = map.NodeId,
                    UplineId = map.UplineId,
                    UplineLeg = map.NodeId,
                    EffectiveDate = map.EffectiveDate
                };
            });
        }

        private async Task ImportNodes(string basePath, string filePath, long treeId, Func<Dictionary<string,string>, Node?> map)
        {
            Console.WriteLine($"Reading Customer Headers");
            var data = _csvFileReader.ReadCsvFile(Path.Combine(basePath, filePath));

            List<Node> nodes = new List<Node>();

            foreach (var row in data)
            {
                var mappedNode = map(row);
                if (mappedNode == null)
                {
                    Console.WriteLine($"Skipping row with missing node data: {string.Join(", ", row.Values)}");
                    continue;
                }

                if (mappedNode.NodeId != null && mappedNode.UplineId != null)
                {
                    if (treeId > 0)
                    {
                        nodes.Add(mappedNode);
                        Console.WriteLine($"Loaded {mappedNode.NodeId} {mappedNode.UplineId}");
                    }
                }
            }

            var ErrorList = new List<ErrorItem>();
            var successCount = 0;

            await Parallel.ForEachAsync(nodes, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (node, cancellationToken) =>
            {
                try
                {
                    await _nodeRepository.InsertNode(treeId, node.NodeId, node.UplineId, node.UplineLeg, node.EffectiveDate);
                    successCount++;
                    Console.WriteLine($"Imported {node.NodeId} {node.UplineId}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new ErrorItem { Message = ex.Message, Items = [node] });
                    }
                }
            });

            var errors = ErrorList.GroupBy(x => x.Message).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var error in errors)
            {
                Console.WriteLine($"Error during import: {error.Key} count: {error.Value.Count}");
            }

            Console.WriteLine($"Imported {successCount} rows");
        }

        private class ErrorItem
        {
            public string Message { get; set; } = "";
            public Node[] Items { get; set; } = [];
        }
    }
}
