using System.Net.Http.Json;

namespace DataLoader.Http
{
    public class Client : IClient
    {
        private readonly HttpClient client = new HttpClient();
        private readonly string _rootUrl = "https://api.pillarshub.com";

        public Client(string apiKey)
        {
            if (!string.IsNullOrWhiteSpace(apiKey)) {
                client.DefaultRequestHeaders.Add("Authorization", apiKey);
            }
        }

        public async Task<string> GetHeaderValue(string url, string headerKey)
        {
            var result = await client.GetAsync(_rootUrl + url);
            var json = await result.Content.ReadAsStringAsync();

            if (result.Headers.TryGetValues(headerKey, out IEnumerable<string> values))
            {
                return values.FirstOrDefault();
            }

            return string.Empty;
        }

        private async Task<T> ProcessResult<T>(HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsStringAsync();

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new System.Exception("Unauthorized");
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                responseMessage.StatusCode == System.Net.HttpStatusCode.Created ||
                responseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default(T);
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new System.Exception("Not Found");
            }

            throw new System.Exception(content);
        }

        public async Task<T> Get<T>(string url)
        {
            var result = await client.GetAsync(_rootUrl + url);
            return await ProcessResult<T>(result);
        }

        public async Task<T> Post<T, R>(string url, R query)
        {
            var result = await client.PostAsJsonAsync(_rootUrl + url, query);
            return await ProcessResult<T>(result);
        }

        public async Task<T> Put<T, R>(string url, R query)
        {
            var result = await client.PutAsJsonAsync(_rootUrl + url, query);
            return await ProcessResult<T>(result);
        }

        public async Task<bool> Delete(string url)
        {
            var result = await client.DeleteAsync(_rootUrl + url);
            await ProcessResult<string>(result);

            return true;
        }
    }
}
