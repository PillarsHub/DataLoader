using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using System.Globalization;

namespace DataLoader.Services.Import
{
    internal class OrderImporter
    {
        private readonly OrderRepository _orderRepository;
        private readonly CSVFileReader _csvFileReader;

        public OrderImporter(CSVFileReader sVFileReader, OrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImportOrders(string orderFile, string lineItemFile)
        {
            var timeZoneId = "Mountain Standard Time";
            var dateFormat = "M/d/yyyy H:mm";

            await Task.CompletedTask;
            var orderRows = _csvFileReader.ReadCsvFile(orderFile);
            var lineItemRows = _csvFileReader.ReadCsvFile(lineItemFile);

            var lineItems = lineItemRows.Select(x =>
            {
                decimal.TryParse(x["CV"], out decimal cv);
                decimal.TryParse(x["QV"], out decimal qv);
                decimal.TryParse(x["price"], out decimal price);
                decimal.TryParse(x["quantity"], out decimal quantity);

                return new OrderLineItem
                {
                    OrderId = x["orderId"],
                    ProductId = x["productId"],
                    Description = x["description"],
                    Price = price,
                    Quantity = quantity,
                    Volume = new[]
                    {
                        new LineItemVolume{ VolumeId = "CV", Volume = cv },
                        new LineItemVolume{ VolumeId = "QV", Volume = qv }
                    }
                };
            }).GroupBy(x => x.OrderId).ToDictionary(x => x.Key, y => y.ToArray());

            var orders = orderRows.Select(x =>
            {
                var orderDate = ReadDate(x["orderDate"], dateFormat, timeZoneId);
                var invoiceDate = ReadDate(x["invoiceDate"], dateFormat, timeZoneId);
                decimal.TryParse(x["subTotal"], out decimal subTotal);
                //decimal.TryParse(x[""], out decimal discount);
                decimal.TryParse(x["shipping"], out decimal shippingCost);
                decimal.TryParse(x["tax"], out decimal taxCost);
                //decimal.TryParse(x[""], out decimal taxRate);
                decimal.TryParse(x["total"], out decimal total);

                decimal.TryParse(x["totalCV"], out decimal totalCV);
                decimal.TryParse(x["totalQV"], out decimal totalQV);

                var ii = x["id"];
                lineItems.TryGetValue(x["id"], out OrderLineItem[]? items);

                if (items != null && items.Length > 0)
                {
                    if (items.Sum(x => x.Volume?.FirstOrDefault(y => y.VolumeId == "CV")?.Volume ?? 0) != totalCV || items.Sum(x => x.Volume?.FirstOrDefault(y => y.VolumeId == "QV")?.Volume ?? 0) != totalQV)
                    {
                        foreach (var item in items)
                        {
                            item.Volume = null;
                        }

                        items.First().Volume = new[] {
                            new LineItemVolume{ VolumeId = "CV", Volume = totalCV },
                            new LineItemVolume{ VolumeId = "QV", Volume = totalQV }
                        };
                    }
                }

                if (!orderDate.HasValue) throw new ArgumentNullException(nameof(orderDate));

                return new Order
                {
                    Id = x["id"],
                    CustomerId = x["customerId"],
                    OrderDate = orderDate.Value,
                    InvoiceDate = invoiceDate,
                    OrderType = x["orderType"],
                    SubTotal = subTotal,
                    //Discount = discount,
                    Shipping = shippingCost,
                    Tax = taxCost,
                    //TaxRate = taxRate,
                    Total = total,
                    Status = x["status"],
                    Tracking = x["tracking"],
                    //Notes = x["Notes"],
                    ShipAddress = new ShipAddress
                    {
                        Line1 = x["shipline1"],
                        Line2 = x["shipline2"],
                        City = x["shipcity"],
                        StateCode = x["shipstateCode"],
                        Zip = x["shipzip"],
                        CountryCode = x["countryCode"],
                    },

                    LineItems = items
                };
            }).ToArray();

            var maxInvoice = orders.MaxBy(x => x.InvoiceDate);
            var maxOrder = orders.MaxBy(x => x.OrderDate);

            List<object> ErrorList = new List<object>();

            await Parallel.ForEachAsync(orders, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (order, cancellationToken) =>
            {
                try
                {
                    await _orderRepository.InsertOrder(order);
                    Console.WriteLine($"Imported {order.Id} {order.CustomerId}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new { msg = ex.Message, order = order });
                    }
                }
            });

            Console.WriteLine($"Imported {orderRows.Count} rows");
        }

        private DateTime? ReadDate(string date, string dateFormat, string timeZoneId)
        {
            if (string.IsNullOrEmpty(date)) return null;

            if (DateTime.TryParseExact(date, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime orderDate))
            {
                // Specify the mountain time zone
                TimeZoneInfo mountainTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

                // Convert to UTC
                DateTime utcOrderDate = TimeZoneInfo.ConvertTimeToUtc(orderDate, mountainTimeZone);

                return utcOrderDate;
            }
            else
            {
                throw new Exception("Failed to parse order date.");
            }
        }
    }
}
