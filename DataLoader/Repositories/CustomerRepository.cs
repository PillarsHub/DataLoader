using DataLoader.Http;
using DataLoader.Repositories.Models;
using DataLoader.Sources;
using DataLoader.TestData;
using DataLoader.Trees;
using System.Web;

namespace DataLoader.Repositories
{
    internal class CustomerRepository 
    {
        private readonly IClient _client;

        public CustomerRepository(IClient client)
        {
            _client = client;
        }

        public async Task<Customer> CreateCustomer(string? nodeId, string uplineId, int? customerType, DateTime date)
        {
            var customer = Customers.GetRandomCustomer(null).ToCustomer(nodeId, date);
            customer.CustomerType = customerType;
            return await CreateCustomer(customer, uplineId);
        }

        public async Task<Customer> CreateCustomer(Customer customer, string uplineId)
        {
            var result = await _client.Post<Customer, Customer>("/api/v1/Customers", customer);
            await _client.Post<Placement, Placement>("/api/v1/Placements", new Placement { NodeId = result.nId, UplineId = uplineId, Date = customer.SignupDate.HasValue ? customer.SignupDate.Value : DateTime.UtcNow });

            return result;
        }

        public async Task<Customer> SaveCustomer(Customer customer, bool exists)
        {
            if (exists)
            {
                return await _client.Put<Customer, Customer>($"/api/v1/Customers/{customer.Id}", customer);
            }
            else
            {
                return await _client.Post<Customer, Customer>($"/api/v1/Customers", customer);
            }
        }

        public async Task InsertSource(Source source)
        {
            await _client.Post<Source, Source>("/api/v1/Sources", source);
        }

        public async Task<Source?> GetSource(string nodeId, string sourceGroupId)
        {
            var sources = await _client.Get<Source[]>($"/api/v1/Sources?nodeId={nodeId}&sourceGroupId={sourceGroupId}");

            return sources.OrderByDescending(x => x.Date).FirstOrDefault();
        }

        public async Task<IEnumerable<Customer>> GetCustomers()
        {
            string customerCountString = await _client.GetHeaderValue("/api/v1/Customers?offset=0&count=1", "Total");

            List<Customer> customers = new List<Customer>();
            int batchSize = 500;
            int offset = 0;
            int.TryParse(customerCountString, out int customerCount);

            while (offset < customerCount)
            {
                customers.AddRange(await _client.Get<Customer[]>($"/api/v1/Customers?offset={offset}&count={batchSize}"));
                offset += batchSize;
            }

            return customers;
        }

        public async Task<bool> DeleteCustomer(string customerId)
        {
            try
            {
                return await _client.Delete($"/api/v1/Customers/{customerId}");
            }
            catch (NotFoundException)
            {
                int rr = 0;
                return true;
            }
        }

        public async Task<Period?> GetPeriod(DateTime date)
        {
            var plans = await _client.Get<Period[]>("/api/v1/CompensationPlans");

            var comPlan = plans.First().Id;
            var dateString = HttpUtility.UrlEncode(date.ToShortDateString());
            var tt = await _client.Get<Period[]>($"/api/v1/CompensationPlans/{comPlan}/Periods?Count=1&BeginDate={dateString}&EndDate={dateString}");
            return tt.FirstOrDefault();
        }
    }
}
