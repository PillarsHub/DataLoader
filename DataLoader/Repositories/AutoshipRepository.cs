using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class AutoshipRepository
    {
        private readonly IClient _client;

        public AutoshipRepository(IClient client)
        {
            _client = client;
        }

        public async Task InsertAutoship(Autoship autoship)
        {
            var result = await _client.Put<Autoship, Autoship>($"/api/v1/Customers/{autoship.CustomerId}/Autoships/{autoship.Id}", autoship);
        }
    }
}
