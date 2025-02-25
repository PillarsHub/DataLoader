using DataLoader.Http;
using DataLoader.Repositories.Models;

namespace DataLoader.Repositories
{
    internal class ProductRepository
    {
        private readonly IClient _client;

        public ProductRepository(IClient client)
        {
            _client = client;
        }

        public async Task InsertProduct(Product product)
        {
            await _client.Put<Product, Product>($"/api/v1/Products/{product.Id}", product);
        }

        public async Task<Product[]> GetAllProducts()
        {
            return await _client.Get<Product[]>($"/api/v1/Products?offset=1&count=600");
        }


        public async Task InsertPrice(ProductPrice price)
        {
            await _client.Put<ProductPrice, ProductPrice>($"/api/v1/Products/{price.ProductId}/Prices/{price.Id}", price);
        }
    }
}
