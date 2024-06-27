using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class SourceGroupRepository
    {
        private readonly IClient _client;

        public SourceGroupRepository(IClient client)
        {
            _client = client;
        }

        public async Task<SourceGroup[]> GetSourceGroups()
        {
            return await _client.Get<SourceGroup[]>($"/api/v1/SourceGroups");
        }
    }
}
