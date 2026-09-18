namespace DataLoader.TestData.Models
{
    internal class CustomerData
    {
        public int CustomerType { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Name 
        {
            set
            {
                var tt = value.Split(" ");
                FirstName = tt[0];
                LastName = tt[1];
            } 
        }
    }
}
