using DataLoader.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataLoader.Trees
{
    class TreeBuilder : ITreeBuilder
    {
        private readonly int _seed = 4432234;
        private readonly IClient _client;

        public TreeBuilder(IClient client)
        {
            _client = client;
        }

        public async Task AddPlacements(IEnumerable<Placement> nodes)
        {
            foreach(var node in nodes)
            {
                await PlaceNode(node);
            }
        }

        public async Task BuildTree(Dictionary<string, string> virtualTree, bool deleteExisting)
        {
            if (deleteExisting)
            {
                int d = 0;
                Console.WriteLine("Deleting Tree");
                var existingTree = await _client.Get<Placement[]>("/Placements");
                foreach (var existing in existingTree)
                {
                    Console.Write("\r{0}% / {1}  ", ++d, existingTree.Length);
                    await _client.Delete($"/Placements/{existing.Id}");
                }
            }

            Console.WriteLine("Building Tree");

            int i = 0;
            foreach (var node in virtualTree.Keys)
            {
                Console.Write("\r{0} / {1}  ", ++i, virtualTree.Keys.Count);
                await PlaceNode(new Placement { NodeId = node, UplineId = virtualTree[node] });
            }
        }

        public Dictionary<string, string> BuildVirtualTree(int numberOfNodes)
        {
            Dictionary<int, int> intNodes = new Dictionary<int, int>();
            Random rnd = new Random(_seed);

            intNodes.Add(1, 0);

            for (int i = 2; i < numberOfNodes + 1; i++)
            {
                intNodes.Add(i, rnd.Next(1, i - 1));
            }

            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var node in intNodes.Keys)
            {
                if (node <= intNodes[node]) throw new Exception();
                result.Add($"N{node}", $"N{intNodes[node]}");
            }

            return result;
        }

        private async Task PlaceNode(Placement placement)
        {
            await _client.Post<Placement, Placement>("/Placements", placement);
        }
    }
}
