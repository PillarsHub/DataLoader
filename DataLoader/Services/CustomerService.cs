using DataLoader.Repositories;
using DataLoader.TestData;

namespace DataLoader.Services
{
    internal class CustomerService
    {
        private readonly CustomerRepository _customerRepository;

        public CustomerService(CustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task CreateCustomers(int count, int customerType, DateTime date)
        {
            List<string> customerIds = new List<string>();
            var existing = await _customerRepository.GetCustomers();
            customerIds.AddRange(existing.Select(x => x.Id));

            for (int i = 0; i < count; i++)
            {
                var uplineId = customerIds.GetRandom("0");
                var customer = Customers.GetRandomCustomer().ToCustomer();
                customer.CustomerType = customerType;

                var customerId = await _customerRepository.CreateCustomer(customer, uplineId);
                customerIds.Add(customerId);
                Console.WriteLine($"Generating customer {i + 1} of {count}: Id:{customerId} uplineId:{uplineId}");
            }
        }
    }
}
