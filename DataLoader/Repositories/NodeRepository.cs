using DataLoader.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataLoader.Repositories
{
    internal class NodeRepository
    {
        private readonly IClient _client;

        public NodeRepository(IClient client)
        {
            _client = client;
        }

        public async Task<Node?> GetNode(long treeId, string nodeId, DateTime? date)
        {
            var datePart = string.Empty;
            if (date.HasValue)
            {
                if (date.Value.Kind == DateTimeKind.Unspecified)
                {
                    date = DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
                }

                var periodDate = date.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                datePart = $"?date={periodDate}";
            }

            try
            {
                var result = await _client.Get<Node>($"/api/v1/Trees/{treeId}/Nodes/{nodeId}{datePart}");
                return result;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            catch
            {
                throw;
            }
        }

        public async Task<Node[]> GetNodes(long treeId, string[] nodeIds, DateTime? date)
        {
            var datePart = string.Empty;
            if (date.HasValue)
            {
                if (date.Value.Kind == DateTimeKind.Unspecified)
                {
                    date = DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
                }

                var periodDate = date.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                datePart = $"&date={periodDate}";
            }
            var idPart = $"?nodeIds=" + string.Join($"&nodeIds=", nodeIds);
            var result = await _client.Get<Node[]>($"/api/v1/Trees/{treeId}/Nodes{idPart}{datePart}&offset=0&count=500");
            return result;
        }

        public async Task<Node[]> GetDownline(long treeId, string nodeId, int levels, DateTime? date)
        {
            var datePart = string.Empty;
            if (date.HasValue)
            {
                if (date.Value.Kind == DateTimeKind.Unspecified)
                {
                    date = DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
                }

                var periodDate = date.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                datePart = $"&date={periodDate}";
            }

            var result = await _client.Get<Node[]>($"/api/v1/Trees/{treeId}/Nodes/{nodeId}/downline?levels={levels}{datePart}&offset=0&count=500");
            return result;
        }

        public async Task InsertNode(long treeId, string nodeId, string uplineId, string uplineLeg, DateTime? effectiveDate)
        {
            var result = await _client.Put<Node, Node>($"/api/v1/Trees/{treeId}/Nodes/{nodeId}", new Node
            {
                NodeId = nodeId,
                UplineId = uplineId,
                UplineLeg = uplineLeg,
                EffectiveDate = effectiveDate
            });

        }
    }

    internal class Node
    {
        public string NodeId { get; set; } = string.Empty;
        public string UplineId { get; set; } = string.Empty;
        public string UplineLeg { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; } = null;
    }
}
