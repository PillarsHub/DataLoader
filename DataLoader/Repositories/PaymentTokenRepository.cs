using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class PaymentTokenRepository
    {
        private readonly IClient _client;

        public PaymentTokenRepository(IClient client)
        {
            _client = client;
        }

        public async Task InsertToken(PaymentToken token)
        {
            var result = await _client.Post<PaymentToken, PaymentToken>($"/api/v1/PaymentTokens", token);
        }
    }
}
