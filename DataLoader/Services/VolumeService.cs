using DataLoader.Repositories;
using DataLoader.Sources;
using DataLoader.TestData;

namespace DataLoader.Services
{
    internal class VolumeService
    {
        private readonly CustomerRepository _customerRepository;
        private readonly SourceGroupRepository _sourceGroupRepository;

        public VolumeService(CustomerRepository customerRepository, SourceGroupRepository sourceGroupRepository)
        {
            _customerRepository = customerRepository;
            _sourceGroupRepository = sourceGroupRepository;
        }

        public async Task CreateVolume(string customerId, double amount, DateTime volumeDate)
        {
            var sourceGroups = await GetSourceGroupVolume();
            var period = await _customerRepository.GetPeriod(volumeDate);
            if (period != null)
            {
                var dates = DateRange(period.Begin, period.End);
                await CreateCustomerVolume(customerId, sourceGroups, amount, dates);
            }
        }

        public async Task CreateVolumes(int count, double amount, DateTime volumeDate)
        {
            List<string> customerIds = new List<string>();
            var sourceGroups = await GetSourceGroupVolume();
            var existing = await _customerRepository.GetCustomers();
            customerIds.AddRange(existing.Select(x => x.Id));

            var period = await _customerRepository.GetPeriod(volumeDate);
            if (period != null)
            {
                var dates = DateRange(period.Begin, period.End);

                for (int i = 0; i < count; i++)
                {
                    var customerId = customerIds.GetRandom("0");
                    await CreateCustomerVolume(customerId, sourceGroups, amount, dates);

                    Console.WriteLine($"Generating volume {i} of {count}: Id:{customerId} uplineId:{amount}");
                }
            }
        }

        private async Task CreateCustomerVolume(string customerId, string[] sourceGroups, double amount, DateTime[] dates)
        {
            var date = dates.GetRandom();
            var orderId = $"{customerId}_{date.Month}{date.Day}{date.Year}{DateTime.UtcNow.Second}";
            try
            {
                foreach(var group in sourceGroups)
                {
                    await _customerRepository.GenerateVolume(new Source
                    {
                        NodeId = customerId,
                        SourceGroupId = group,
                        Value = amount,
                        Date = date,
                        ExternalId = orderId
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task<string[]> GetSourceGroupVolume()
        {
            var allSourceGroups = await _sourceGroupRepository.GetSourceGroups();
            return allSourceGroups.Where(x =>
            {
                if (x.Id == "MBonus") return false;
                if (x.SourceType != "SumValue") return false;
                if (x.DataType != "Decimal") return false;
                return true;
            }).Select(x => x.Id).ToArray();
        }

        private DateTime[] DateRange(DateTime begin, DateTime end)
        {
            List<DateTime> dates = new List<DateTime>();
            var date = begin.ToUniversalTime();

            while(date < end)
            {
                dates.Add(date);
                date = date.AddDays(1);
            }

            dates.Add(end.ToUniversalTime());
            return dates.ToArray();
        }
    }
}
