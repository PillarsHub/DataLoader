using System.Threading.Tasks;

namespace DataLoader.Http
{
    public interface IClient
    {
        Task<string> GetHeaderValue(string url, string headerKey);
        Task<T> Get<T>(string url);

        Task<T> Post<T, R>(string url, R query);
        Task<T> Put<T, R>(string url, R query);
        Task<bool> Delete(string url);
    }
}
