using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class OrderRepository
    {
        private readonly IClient _client;

        public OrderRepository(IClient client)
        {
            _client = client;
        }

        public async Task InsertOrder(Order order)
        {
            var result = await _client.Put<Order, Order>($"/api/v1/Orders/{order.Id}", order);
        }
    }
}
