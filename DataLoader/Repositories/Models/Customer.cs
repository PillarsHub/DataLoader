namespace DataLoader.Repositories.Models
{
    internal class Customer
    {
        public string? Id { get; set; }
        public List<string> ExternalIds { get; set; } = Array.Empty<string>().ToList();
        public int? CustomerType { get; set; } = null;
        public DateTime? SignupDate { get; set; } = null;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public List<Address> Addresses { get; set; } = Array.Empty<Address>().ToList();
        public List<PhoneNumber> PhoneNumbers { get; set; } = Array.Empty<PhoneNumber>().ToList();
        public string EmailAddress { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; } = null;
        public string? ProfileImage { get; set; } = null;
        public string? WebAlias { get; set; } = null;

        internal string nId
        {
            get { return Id ?? string.Empty; }
        }
    }

    internal class Address
    {
        public string Type { get; set; } = string.Empty;
        public string Line1 { get; set; } = string.Empty;
        public string Line2 { get; set; } = string.Empty;
        public string Line3 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }

    public class PhoneNumber
    {
        public string Number { get; set; }
        public string Type { get; set; }
        public string CountryCode { get; set; }
    }
}
