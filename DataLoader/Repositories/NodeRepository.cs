using DataLoader.Http;

namespace DataLoader.Repositories
{
    internal class NodeRepository
    {
        private readonly IClient _client;

        public NodeRepository(IClient client)
        {
            _client = client;
        }

        public async Task<Node> GetNode(long treeId, string nodeId)
        {
            var result = await _client.Get<Node>($"/api/v1/Trees/{treeId}/Nodes/{nodeId}");
            return result;
        }

        public async Task InsertNode(long treeId, string nodeId, string uplineId, string uplineLeg)
        {
            var result = await _client.Put<Node, Node>($"/api/v1/Trees/{treeId}/Nodes/{nodeId}", new Node
            {
                NodeId = nodeId,
                UplineId = uplineId,
                UplineLeg = uplineLeg
            });

        }
    }

    internal class Node
    {
        public string NodeId { get; set; } = string.Empty;
        public string UplineId { get; set; } = string.Empty;
        public string UplineLeg { get; set; } = string.Empty;
    }
}
