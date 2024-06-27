namespace DataLoader.TestData.Models
{
    internal class CustomerData
    {
        public int CustomerType { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

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
