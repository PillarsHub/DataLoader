using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class HistoricalValueRepository
    {
        private readonly IClient _client;

        public HistoricalValueRepository(IClient client)
        {
            _client = client;
        }

        public async Task InsertValue(HistoricalValue value)
        {
            var result = await _client.Post<HistoricalValue, HistoricalValue>($"/api/v1/HistoricalValues", value);
        }
    }
}
