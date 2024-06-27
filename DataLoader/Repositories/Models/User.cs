namespace DataLoader.Repositories.Models
{
    internal class User
    {
        public AuthToken AuthToken { get; set; }
    }

    internal class AuthToken
    {
        public string Token { get; set;} = string.Empty;
        public long? EnvironmentId { get; set; }
        public string? EnvironmentName { get; set; }
    }
}
