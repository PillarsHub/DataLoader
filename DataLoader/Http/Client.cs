using DataLoader.Repositories.Models;
using System.Net.Http.Json;

namespace DataLoader.Http
{
    public class Client : IClient
    {
        private readonly HttpClient client = new HttpClient();
        private readonly string _rootUrl = "https://api.pillarshub.com";
        private DateTime _expireTime = DateTime.MinValue;
        private string _currentToken = string.Empty;

        public Client(string apiKey)
        {
            if (!string.IsNullOrWhiteSpace(apiKey)) {
                client.DefaultRequestHeaders.Add("Authorization", apiKey);
                _currentToken = apiKey;
                _expireTime = DateTime.Now.AddMinutes(15);
            }
        }

        private async Task CheckToken()
        {
            if (DateTime.Now > _expireTime && !string.IsNullOrWhiteSpace(_currentToken))
            {
                var newToken = await client.GetAsync(_rootUrl + "/Authentication/refresh/" + _currentToken);
                var user = await ProcessResult<User>(newToken);
                if (user != null)
                {
                    _currentToken = user.AuthToken.Token;
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", _currentToken);
                    _expireTime = DateTime.Now.AddMinutes(15);
                }
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
                throw new Exception("Not Found");
            }

            throw new Exception(content);
        }

        public async Task<T> Get<T>(string url, int retry = 0)
        {
            try
            {
                await CheckToken();

                var result = await client.GetAsync(_rootUrl + url);
                return await ProcessResult<T>(result);
            }
            catch
            {
                if (retry < 5)
                {
                    await Task.Delay(500);
                    return await Get<T>(url, retry + 1);
                }
                throw;
            }
        }

        public async Task<T> Post<T, R>(string url, R query)
        {
            await CheckToken();

            var result = await client.PostAsJsonAsync(_rootUrl + url, query);
            return await ProcessResult<T>(result);
        }

        public async Task<T> Put<T, R>(string url, R query)
        {
            await CheckToken();

            var result = await client.PutAsJsonAsync(_rootUrl + url, query);
            return await ProcessResult<T>(result);
        }

        public async Task<bool> Delete(string url)
        {
            await CheckToken();

            var result = await client.DeleteAsync(_rootUrl + url);
            await ProcessResult<string>(result);

            return true;
        }
    }
}
