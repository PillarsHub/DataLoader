using DataLoader.Http;
using DataLoader.Repositories.Models;
using DataLoader.Services;
using Microsoft.Extensions.DependencyInjection;

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
            Console.WriteLine($"Connected to '{user.AuthToken.EnvironmentName}'");

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
                else if (input == "v")
                {
                    var volumeService = serviceProvider.GetRequiredService<VolumeService>();
                    await CreateVolumes(volumeService);
                }
                else if (input == "help")
                {
                    Console.WriteLine("'(C)ustomers' - Generate test Customers.");
                    Console.WriteLine("(V)olume - Generate volume for existing customers");
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

        private static async Task GenerateCustomers(CustomerService customerService)
        {
            Console.WriteLine("How many customers would you like to generate?");
            Console.Write("> ");
            var count = ReadInt();
            Console.WriteLine("What is the customer type to generate?");
            Console.Write("> ");
            var customerType = ReadInt();
            Console.WriteLine("What period would you like to generate volume for? (Press Enter for current period)");
            Console.Write("> ");
            var date = ReadDate();
            if (count > 0)
            {
                await customerService.CreateCustomers(count, customerType, date);
            }
        }

        private static async Task CreateVolumes(VolumeService volumeService)
        {
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
            var date = ReadDate();

            if (string.IsNullOrWhiteSpace(customerId))
            {
                await volumeService.CreateVolumes(count, volumeAmount, date);
            }
            else
            {
                await volumeService.CreateVolume(customerId, volumeAmount, date);
            }
        }

        private static async Task<User> Login()
        {
            User user = null;
            Console.WriteLine("Welcome to the data Loader. Please login.");
            while (user == null)
            {
                Console.Write("Username:");
                var username = Console.ReadLine();
                Console.Write("Password:");
                var password = ReadPassword();

                try
                {
                    var client = new Client("");
                    user = await client.Get<User>($"/Authentication?username={username}&password={password}");
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

                var envIndex = Console.ReadLine();
                var env = environments.FirstOrDefault(x => x.Id.ToString() == envIndex);
                if (env != null)
                {
                    user = await envClient.Get<User>($"/Authentication/refresh/{user.AuthToken.Token}?environmentId={env.Id}");
                }
            }

            var selEnv = environments.FirstOrDefault(x => x.Id == user.AuthToken.EnvironmentId);
            user.AuthToken.EnvironmentName = selEnv.Name;

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

        private static DateTime ReadDate()
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) input = DateTime.Now.ToShortDateString();
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
