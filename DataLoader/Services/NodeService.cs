using DataLoader.Repositories;

namespace DataLoader.Services
{
    internal class NodeService
    {
        private readonly NodeRepository _nodeRepository;

        public NodeService(NodeRepository nodeRepository)
        {
            _nodeRepository = nodeRepository;
        }

        public async Task<Node?> GetNode(long treeId, string nodeId, DateTime? date)
        {
            return await _nodeRepository.GetNode(treeId, nodeId, date);
        }

        public async Task InsertNode(long treeId, string nodeId, string uplineId, string uplineLeg, DateTime? effectiveDate)
        {
            await _nodeRepository.InsertNode(treeId, nodeId, uplineId, uplineLeg, effectiveDate);
        }

        public async Task<string?> FindRandomDownline(long treeId, string nodeId, string uplineId, int maxGeneration)
        {
            var downline = await _nodeRepository.GetDownline(treeId, uplineId, maxGeneration, null);
            var node = GetRandomNode(downline, nodeId);
            return node?.NodeId;
        }

        private static Node? GetRandomNode(Node[] nodes, string downlineId)
        {
            if (nodes == null || nodes.Length == 0)
                return null;

            // Build lookup: UplineId -> direct children
            var childrenByUpline = nodes
                .GroupBy(x => x.UplineId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList(),
                    StringComparer.OrdinalIgnoreCase);

            // downlineId itself is excluded.
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { downlineId };

            // Find every node below downlineId.
            var queue = new Queue<string>();
            queue.Enqueue(downlineId);

            while (queue.Count > 0)
            {
                var parentId = queue.Dequeue();

                if (!childrenByUpline.TryGetValue(parentId, out var children))
                    continue;

                foreach (var child in children)
                {
                    // Add returns false if we've already seen this node.
                    if (excluded.Add(child.NodeId))
                    {
                        queue.Enqueue(child.NodeId);
                    }
                }
            }

            var available = nodes
                .Where(x => !excluded.Contains(x.NodeId))
                .ToArray();

            if (available.Length == 0)
                return null;

            return available[Random.Shared.Next(available.Length)];
        }
    }
}
