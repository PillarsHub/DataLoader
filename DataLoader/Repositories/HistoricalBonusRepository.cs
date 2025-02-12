using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class HistoricalBonusRepository
    {
        private readonly IClient _client;

        public HistoricalBonusRepository(IClient client)
        {
            _client = client;
        }

        public async Task InsertValue(HistoricalBonus value)
        {
            var result = await _client.Post<HistoricalBonus, HistoricalBonus>($"/api/v1/HistoricalBonuses", value);
        }
    }
}
