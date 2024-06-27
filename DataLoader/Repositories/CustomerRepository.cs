﻿using DataLoader.Http;
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

        public async Task CreateCustomer(string nodeId, string uplineId, DateTime date)
        {
            var customer = Customers.GetRandomCustomer().ToCustomer(nodeId, date);
            await CreateCustomer(customer, uplineId);
        }

        public async Task<string> CreateCustomer(Customer customer, string uplineId)
        {
            var result = await _client.Post<Customer, Customer>("/api/v1/Customers", customer);
            await _client.Post<Placement, Placement>("/api/v1/Placements", new Placement { NodeId = result.Id, UplineId = uplineId, Date = customer.SignupDate.HasValue ? customer.SignupDate.Value : DateTime.UtcNow });

            return result.Id;
        }

        public async Task GenerateVolume(Source source)
        {
            await _client.Post<Source, Source>("/api/v1/Sources", source);
        }

        public async Task<IEnumerable<Customer>> GetCustomers()
        {
            string customerCountString = await _client.GetHeaderValue("/api/v1/Customers?offset=0&count=1", "Total");

            List<Customer> customers = new List<Customer>();
            int batchSize = 100;
            int offset = 0;
            int.TryParse(customerCountString, out int customerCount);

            while (offset < customerCount)
            {
                customers.AddRange(await _client.Get<Customer[]>($"/api/v1/Customers?offset={offset}&count={batchSize}"));
                offset += batchSize;
            }

            return customers;
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
