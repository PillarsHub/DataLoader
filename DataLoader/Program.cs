using DataLoader.Http;
using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using DataLoader.Services;
using DataLoader.Services.Import;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Web;

namespace DataLoader
{
    public class Prorgam
    {
        public static async Task Main(string[] args)
        {
            User user = await Login();
            var services = new ServiceCollection();
            Startup.ConfigureServices(services, user.AuthToken.Token);
            using var serviceProvider = services.BuildServiceProvider();

            Console.Clear();
            Console.WriteLine($"Connected as '{user.Username}' to '{user.AuthToken.EnvironmentId} - {user.AuthToken.EnvironmentName}'");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("type 'help' for a list of commands.");
                Console.Write("> ");
                var input = Console.ReadLine()?.ToLower() ?? string.Empty;
                Console.WriteLine();

                if (input == "x" || input == "exit")
                {
                    break;
                }
                else if (input == "c")
                {
                    var customerService = serviceProvider.GetRequiredService<CustomerService>();
                    await GenerateCustomers(customerService);
                }
                else if (input == "m")
                {
                    Console.WriteLine("(G)enerate a new Model");
                    Console.WriteLine("(I)mporto a new Model");
                    Console.Write("> ");
                    var modelMethod = Console.ReadLine()?.ToLower() ?? string.Empty;
                    if (modelMethod == "g")
                    {
                        var customerService = serviceProvider.GetRequiredService<CustomerService>();
                        await GenerateModel(customerService);
                    }
                    else
                    {
                        var importer2 = serviceProvider.GetRequiredService<ImportManager>();
                        await importer2.ImportModel();
                    }
                }
                else if (input == "v")
                {
                    var volumeService = serviceProvider.GetRequiredService<VolumeService>();
                    await CreateVolumes(volumeService);
                }
                else if (input == "o")
                {
                    var orderService = serviceProvider.GetRequiredService<OrderService>();
                    await CreateOrders(orderService);
                }
                else if (input == "i")
                {
                    var importer = serviceProvider.GetRequiredService<ImportManager>();
                    await importer.BeginImport();
                }
                else if (input == "d")
                {
                    var deleteDataManager = serviceProvider.GetRequiredService<DeleteDataManager>();
                    await deleteDataManager.BeginDelete();
                }
                else if (input == "u")
                {
                    var productRepository = serviceProvider.GetRequiredService<ProductRepository>();
                    var products = await productRepository.GetAllProducts();

                    //var imageUrls = products.Select(x => x.ImageUrl ?? string.Empty).Distinct().Where(x => x.Contains('\t')).ToList();
                    var imageUrls = new List<string>(new[] { "https://commonsense.corpadmin.directscale.com/CMS/Images/Inventory/0ABE80C4-47CF-462B-A807-2EC5BEB849F1.jpeg" });

                    var uploader = serviceProvider.GetRequiredService<ImageUploader>();
                    await uploader.UploadImagesAsync(
                        imageUrls: imageUrls,
                        apiUrl: "/api/v1/Blobs",
                        category: "inventory"
                    );
                }
                else if (input == "help")
                {
                    Console.WriteLine("(C)ustomers - Generate test Customers.");
                    Console.WriteLine("(V)olume - Generate volume for existing customers");
                    Console.WriteLine("(M)odel - Generate or Import a Model data for compensation plan modeling");
                    Console.WriteLine("(O)rders - Generate test Orders.");
                    Console.WriteLine("(I)mport Data - Import data from a CSV file.");
                    Console.WriteLine("(D)elete Data - Bulk delete data");
                    Console.WriteLine("exit - Exit Pillars Data Loader.");
                }
                else
                {
                    Console.WriteLine("Unrecognised Key");
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Exiting...");
        }

        private static string ReadPassword()
        {
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            return pass;
        }

        private static Distribution[]? BuildDistribution(string? distributionInput)
        {
            if (string.IsNullOrWhiteSpace(distributionInput)) return null;
            
            var results  = new List<Distribution>();
            foreach (var part in distributionInput.Split(","))
            {
                if (!part.Contains(":")) return null;

                var colPos = part.IndexOf(":");
                var first = part.Substring(0, colPos);
                var second = part.Substring(colPos + 1);

                int.TryParse(first.Replace("%", ""), out var percent);
                int.TryParse(second, out var amount);

                if (percent == 0) return null;
                if (amount == 0) return null;

                results.Add(new Distribution
                {
                    Percent = percent,
                    Amount = amount
                });
            }

            if (results.Sum(d => d.Percent) != 100) return null;

            return results.Count > 0 ? results.ToArray() : null;
        }

        private static async Task GenerateModel(CustomerService customerService)
        {
            Console.WriteLine("How many customers would you like to generate?");
            Console.Write("> ");
            var count = ReadInt();

            Console.WriteLine("Please enter the BEGIN date");
            Console.Write("> ");
            var beginDate = ReadDate(DateTime.UtcNow);

            Console.WriteLine("Please enter the END date");
            Console.Write("> ");
            var endDate = ReadDate(DateTime.UtcNow);

            Console.WriteLine("What is the customer type percentages would you like to generate?");
            Distribution[]? customerTypeDistribution = null;
            do
            {
                Console.WriteLine("Enter percent:Type, percent:Type  Example: (50%:1,50%:2)");
                Console.Write("> ");
                var custTypeDistInput = Console.ReadLine();
                customerTypeDistribution = BuildDistribution(custTypeDistInput);
            } while (customerTypeDistribution == null);


            Console.WriteLine("How many orders per customers would you like to generate?");
            Console.Write("> ");
            var numberOfOrders = ReadInt();
            var orders = new List<(DateTime Begin, DateTime End, Distribution[] Distributions)>();
            string[]? volumeKeys = null;

            for (int i = 0; i < numberOfOrders; i++)
            {

                Console.WriteLine("Please enter the BEGIN date (Press Enter to use customer date)");
                Console.Write("> ");
                var oBeginDate = ReadDate(beginDate);

                Console.WriteLine("Please enter the END date (Press Enter to use customer date)");
                Console.Write("> ");
                var oEndDate = ReadDate(endDate);

                Console.WriteLine("What is the volume key would you like to generate? (Press Enter to use CV,QV)");
                var volumeKey = Console.ReadLine() ?? string.Empty;
                volumeKeys = string.IsNullOrWhiteSpace(volumeKey) ? null : [volumeKey];

                Console.WriteLine("What is the volume percentages would you like to generate?");
                Distribution[]? volumeDistribution = null;
                do
                {
                    Console.WriteLine("Enter percent:volumeAmount,percent:volumeAmount  Example: (50%:100,50%:200)");
                    Console.Write("> ");
                    var volumeDistInput = Console.ReadLine();
                    volumeDistribution = BuildDistribution(volumeDistInput);
                } while (volumeDistribution == null);

                orders.Add((oBeginDate, oEndDate, volumeDistribution));
            }

            await customerService.CreateModel(count, beginDate, endDate, customerTypeDistribution, volumeKeys, orders.ToArray());
        }

        private static async Task GenerateCustomers(CustomerService customerService)
        {
            var customerIds = new ConcurrentDictionary<string, Customer>();

            Console.WriteLine("How many customers would you like to generate?");
            Console.Write("> ");
            var count = ReadInt();
            Console.WriteLine("What is the customer type to generate?");
            Console.Write("> ");
            var customerType = ReadInt();
            Console.WriteLine("What period would you like to generate volume for? (Press Enter for current period)");
            Console.Write("> ");
            var date = ReadDate(DateTime.UtcNow);

            Console.WriteLine($"Do you wish to assign a sponsor (y/n)");
            Console.Write("> ");
            var useSponsor = Console.ReadLine() ?? string.Empty;
            if (useSponsor.ToLower() == "y")
            {
                Console.WriteLine("Enter upline Id");
                var uplineId = Console.ReadLine();

                Console.WriteLine("Would you like to stack the new customers under each new one created (y/n)");
                var stack = Console.ReadLine()?.ToLower() == "y";

                await customerService.CreateCustomers(count, customerType, uplineId, stack, date, null, null, customerIds);
            }
            else if (count > 0)
            {
                await customerService.CreateCustomers(count, customerType, null, false, date, null, null, customerIds);
            }
        }

        private static async Task CreateOrders(OrderService orderService)
        {
            Console.WriteLine("How many orders would you like to generate?");
            Console.Write("> ");
            var count = ReadInt();
            Console.WriteLine("What period would you like to generate volume for? (Press Enter for current period)");
            Console.Write("> ");
            var date = ReadDate(DateTime.UtcNow);
            Console.WriteLine($"Do you wish to assign a customer Id (y/n)");
            Console.Write("> ");
            var useSponsor = Console.ReadLine() ?? string.Empty;
            if (useSponsor.ToLower() == "y")
            {
                Console.WriteLine("Enter customerId Id");
                var customerId = Console.ReadLine();
                await orderService.CreateOrders(count, customerId, date);
            }
            else if (count > 1)
            {
                await orderService.CreateOrders(count, null, date);
            }
        }

        private static async Task CreateVolumes(VolumeService volumeService)
        {
            var sourceGroups = await volumeService.GetSourceGroupVolume();
            foreach(var sg in sourceGroups)
            {
                Console.WriteLine($"{sg.Id} - {sg.SourceType}");
            }
            Console.WriteLine("What volume would you like to generate? (You can select multple by seperating with a comma)");
            Console.Write("> ");
            var volumeKeys = Console.ReadLine()?.Split(",") ?? [];

            Console.WriteLine("How many customers would you like to generate volume for?");
            Console.Write("> ");
            var count = ReadInt();
            string customerId = null;
            if (count == 1)
            {
                Console.WriteLine("What customer Id would you like to generate volume for?");
                Console.Write("> ");
                customerId = Console.ReadLine();
            }
            Console.WriteLine("How much volume per customer?");
            Console.Write("> ");
            var volumeAmount = ReadInt();
            Console.WriteLine("What period would you like to generate volume for? (Press Enter for current period)");
            Console.Write("> ");
            var date = ReadDate(DateTime.UtcNow);

            if (string.IsNullOrWhiteSpace(customerId))
            {
                await volumeService.CreateVolumes(count, volumeKeys, volumeAmount, date);
            }
            else
            {
                await volumeService.CreateVolume(customerId, volumeKeys, volumeAmount, date);
            }
        }

        private static async Task<User> Login(string? defUsername = null, string? defPassword = null, string? defEnv = null)
        {
            User? user = null;
            Console.WriteLine("Welcome to the data Loader. Please login.");
            while (user == null)
            {
                Console.Write("Username:");
                var username = defUsername ?? Console.ReadLine();

                Console.Write("Password:");
                var password = defPassword ?? ReadPassword();

                try
                {
                    var client = new Client("");
                    user = await client.Get<User>($"/Authentication?username={HttpUtility.UrlEncode(username)}&password={HttpUtility.UrlEncode(password)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(ex.Message);
                }
            }

            var envClient = new Client(user.AuthToken.Token);
            var environments = await envClient.Get<EnvironmentItem[]>($"/Authentication/token/{user.AuthToken.Token}/Environments");

            while (!user.AuthToken.EnvironmentId.HasValue)
            {
                Console.WriteLine($"");
                Console.WriteLine($"Please select an environment");

                foreach (var item in environments)
                {
                    Console.WriteLine($"{item.Id} - {item.Name}");
                }

                var envIndex = defEnv ?? Console.ReadLine();
                var env = environments.FirstOrDefault(x => x.Id.ToString() == envIndex);
                if (env != null)
                {
                    user = await envClient.Get<User>($"/Authentication/refresh/{user.AuthToken.Token}?environmentId={env.Id}");
                }
            }

            var selEnv = environments.FirstOrDefault(x => x.Id == user.AuthToken.EnvironmentId);
            user.AuthToken.EnvironmentName = selEnv?.Name;

            return user;
        }

        private static int ReadInt()
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (int.TryParse(input, out int outVal))
                {
                    return outVal;
                }
            }
        }

        private static DateTime ReadDate(DateTime defDate)
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) input = defDate.ToShortDateString();
                if (DateTime.TryParse(input, out DateTime outVal))
                {
                    return DateTime.SpecifyKind(outVal, DateTimeKind.Utc);
                }
            }
        }

        //private static async Task EraseAllData(IServiceProvider serviceProvider)
        //{
        //    Console.WriteLine($"Resetting Database.");

        //    var connectionService = serviceProvider.GetService<IConnectionService>();
        //    using (var connection = connectionService.GetConnection())
        //    {
        //        var sql = "delete from Sources; " +
        //            "delete from CommissionValues; " +
        //            "delete from Placements; " +
        //            "delete from Nodes; " +
        //            "delete from Bonuses; " +
        //            "delete from BonusDetail; " +
        //            "delete from BonusEarned; ";

        //        connection.Open();

        //        await connection.ExecuteAsync(sql);
        //    }
        //}
    }
}
