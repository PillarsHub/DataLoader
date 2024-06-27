using System;

namespace DataLoader.Repositories.Models
{
    internal class Customer
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public int? CustomerType { get; set; }
        public string Status { get; set; }
        public string EmailAddress { get; set; }
        public string Language { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? SignupDate { get; set; }
        public string ProfileImage { get; set; }
    }
}
