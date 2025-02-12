using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class TreeRepository
    {
        private readonly IClient _client;

        public TreeRepository(IClient client)
        {
            _client = client;
        }

        public async Task<Tree[]> GetTrees()
        {
            return await _client.Get<Tree[]>("/api/v1/Trees");
        }
    }
}
