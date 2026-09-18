using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using DataLoader.TestData;
using System.Collections.Concurrent;

namespace DataLoader.Services
{
    internal class CustomerService
    {
        private readonly CustomerRepository _customerRepository;
        private readonly OrderService _orderService;
        private readonly NodeRepository _nodeRepository;

        public CustomerService(CustomerRepository customerRepository, OrderService orderService, NodeRepository nodeRepository)
        {
            _customerRepository = customerRepository;
            _orderService = orderService;
            _nodeRepository = nodeRepository;
        }

        public async Task CreateCustomer(string? Id, string? firstName, string? lastName, int customerType, IEnumerable<(long treeId, string upline)>? uplineIds, IEnumerable<(string Key, decimal Volume)>? volumes, ConcurrentDictionary<string, Customer> customerIds)
        {
            if (customerIds.Count == 0)
            {
                var existing = await _customerRepository.GetCustomers();
                foreach (var item in existing)
                {
                    customerIds.TryAdd(item.Id ?? string.Empty, item);
                }
            }

            var customer = Customers.GetRandomCustomer(customerIds.Values.ToList()).ToCustomer(date: DateTime.UtcNow);

            if (Id != null && customerIds.ContainsKey(Id))
            {
                customer = customerIds[Id];
            }
            
            customer.CustomerType = customerType;
            if (!string.IsNullOrWhiteSpace(Id)) customer.Id = Id;
            if (!string.IsNullOrWhiteSpace(firstName)) customer.FirstName = firstName;
            if (!string.IsNullOrWhiteSpace(lastName)) customer.LastName = lastName;
            var newCust = await _customerRepository.SaveCustomer(customer);
            Console.WriteLine($"Generating customer: Id:{newCust.Id} Name:{newCust.FirstName} {newCust.LastName}");

            if (uplineIds != null) 
            {
                foreach (var item in uplineIds)
                {
                    var newCustId = newCust.Id ?? string.Empty;
                    await _nodeRepository.InsertNode(item.treeId, newCustId, item.upline, newCustId, null);
                }
            }

            if (volumes != null)
            {
                var filteredVols = volumes.Where(x => x.Volume != 0).ToList();
                if (filteredVols.Count > 0)
                {
                    await _orderService.CreateOrder(newCust.Id ?? string.Empty, DateTime.UtcNow, filteredVols);
                }
            }

            customerIds.TryAdd(newCust.Id ?? string.Empty, newCust);
        }

        public async Task CreateCustomers(int count, int customerType, string? uplineId, bool stack, DateTime date, string[]? volumeKeys, (DateTime Date, decimal Volume)[]? volumes, ConcurrentDictionary<string, Customer> customerIds)
        {
            //List<string> customerIds = new List<string>();
            if (customerIds.Count == 0)
            {
                var existing = await _customerRepository.GetCustomers();
                foreach (var item in existing)
                {
                    customerIds.TryAdd(item.Id ?? string.Empty, item);
                }
            }

            for (int i = 0; i < count; i++)
            {
                uplineId = uplineId ?? customerIds.Keys.ToList().GetRandom("0");
                try
                {
                    var customer = Customers.GetRandomCustomer(customerIds.Values.ToList()).ToCustomer(date: date);
                    customer.CustomerType = customerType;
                    var newCust = await _customerRepository.CreateCustomer(customer, uplineId);

                    if (volumes != null)
                    {
                        foreach (var volume in volumes)
                        {
                            var volKeys = volumeKeys == null ? ["CV", "QV"] : volumeKeys;
                            var value = volKeys.Select(x => (x, volume.Volume)).ToArray();
                            await _orderService.CreateOrder(newCust.Id ?? string.Empty, volume.Date, value);
                        }
                    }

                    customerIds.TryAdd(newCust.Id ?? string.Empty, newCust);
                    Console.WriteLine($"Generating customer {i + 1} of {count}: Id:{newCust.Id} uplineId:{uplineId}");
                    if (stack) uplineId = newCust.Id;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }



        public async Task CreateModel(int count, DateTime begin, DateTime end, Distribution[] typeDistribution, string[]? volumeKeys, (DateTime Begin, DateTime End, Distribution[] Distributions)[] orders)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (typeDistribution.Length == 0) throw new ArgumentException("typeDistribution required");
            if (orders.Length == 0) throw new ArgumentException("volumeDistribution required");

            // Ensure percentages are sensible (sum to 100). If you want to be permissive,
            // you can normalize instead—this implementation enforces exactly 100.
            if (typeDistribution.Sum(d => d.Percent) != 100)
                throw new ArgumentException("typeDistribution percents must sum to 100.");

            var rng = Random.Shared;
            var orderDistDict = new ConcurrentDictionary<int, List<int>>();

            for (int i = 0; i < orders.Length; i++)
            {
                if (orders[i].Distributions.Sum(d => d.Percent) != 100)
                    throw new ArgumentException("volumeDistribution percents must sum to 100.");

                var volumeValues = BuildExactDistribution(orders[i].Distributions, count);
                Shuffle(volumeValues, rng);
                orderDistDict.TryAdd(i, volumeValues);
            }

            var typeValues = BuildExactDistribution(typeDistribution, count);
            Shuffle(typeValues, rng);

            var dates = DateRange(begin, end);
            var customerIds= new ConcurrentDictionary<string, Customer>();

            var existing = await _customerRepository.GetCustomers();
            foreach (var item in existing)
            {
                customerIds.TryAdd(item.Id ?? string.Empty, item);
            }

            var maxDegreeOfParallelism = Environment.ProcessorCount;

            await Parallel.ForEachAsync(Enumerable.Range(0, count), new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, async (i, ct) =>
            {
                var volumes = new List<(DateTime date, decimal volume)>();
                for (int oIndex = 0; oIndex < orders.Length; oIndex++)
                {
                    var orderDates = DateRange(orders[oIndex].Begin, orders[oIndex].End);
                    volumes.Add((orderDates.GetRandom(), orderDistDict[oIndex][i]));
                }

                await CreateCustomers(1, typeValues[i], null, false, dates.GetRandom(), volumeKeys, volumes.ToArray(), customerIds);
            });
        }


        private static List<int> BuildExactDistribution(Distribution[] dist, int count)
        {
            // First pass: floor allocations
            var allocations = new (int amount, int exact, int floor, int remainder)[dist.Length];
            int totalAllocated = 0;

            for (int i = 0; i < dist.Length; i++)
            {
                // exact = count * percent / 100 in hundredths to keep remainders
                int exactHundredths = count * dist[i].Percent; // represents * /100 but keep remainder
                int floor = exactHundredths / 100;
                int remainder = exactHundredths % 100;

                allocations[i] = (dist[i].Amount, exactHundredths, floor, remainder);
                totalAllocated += floor;
            }

            // Distribute leftover by largest remainders
            int leftover = count - totalAllocated;
            if (leftover > 0)
            {
                foreach (var idx in allocations
                                     .Select((x, i) => (i, x.remainder))
                                     .OrderByDescending(t => t.remainder)
                                     .ThenBy(_ => Guid.NewGuid()) // tie-breaker randomness
                                     .Take(leftover)
                                     .Select(t => t.i))
                {
                    allocations[idx].floor++;
                }
            }

            // Build the final value list
            var values = new List<int>(count);
            foreach (var (amount, _, floor, _) in allocations)
            {
                for (int k = 0; k < floor; k++)
                    values.Add(amount);
            }

            // Defensive: ensure exact length
            if (values.Count != count)
            {
                // This should not happen, but if it does due to input weirdness, adjust.
                while (values.Count < count) values.Add(allocations[0].amount);
                if (values.Count > count) values.RemoveRange(count, values.Count - count);
            }

            return values;
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private DateTime[] DateRange(DateTime begin, DateTime end)
        {
            List<DateTime> dates = new List<DateTime>();
            var date = begin.ToUniversalTime();

            while (date < end)
            {
                dates.Add(date);
                date = date.AddDays(1);
            }

            dates.Add(end.ToUniversalTime());
            return dates.ToArray();
        }

    }
}
