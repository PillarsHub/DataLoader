using DataLoader.Importer;
using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using Importer.Contracts;

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

        public async Task ImportOrders(string basePath, IImportProfile<OrderHeaderImportRequest>? headerProfile,
            IImportProfile<OrderLineItemImportRequest>? lineItemProfile, IImportProfile<OrderVolumeImportRequest>? volumeProfile,
            IImportProfile<OrderPaymentImportRequest>? paymentProfile)
        {
            if (headerProfile == null || lineItemProfile == null)
            {
                Console.WriteLine($"No order map to import.");
                return;
            }

            var paymentRows = new List<Dictionary<string, string>>();
            var volumeRows = new List<Dictionary<string, string>>();

            Console.WriteLine($"Reading Order Headers");
            var orderRows = _csvFileReader.ReadCsvFile(Path.Combine(basePath, headerProfile.SourceFile));

            Console.WriteLine($"Reading Order LineItems");
            var lineItemRows = _csvFileReader.ReadCsvFile(Path.Combine(basePath, lineItemProfile.SourceFile));

            Console.WriteLine($"Reading Order Volume");
            if (volumeProfile != null) volumeRows = _csvFileReader.ReadCsvFile(Path.Combine(basePath, volumeProfile.SourceFile));
            
            Console.WriteLine($"Reading Order Payments");
            if (paymentProfile != null) paymentRows = _csvFileReader.ReadCsvFile(Path.Combine(basePath, paymentProfile.SourceFile));

            var lineItemsByOrderId = lineItemRows.Select(x => lineItemProfile.Map(x))
                .GroupBy(x => x.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var paymentsByOrderId = paymentRows.Select(x => paymentProfile?.Map(x))
                .GroupBy(x => x.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var orderVolume = volumeRows.Select(x => volumeProfile?.Map(x))
                .GroupBy(x => x.OrderId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.Volume);

            var orders = new List<Order>();
            Console.WriteLine($"Building orders");
            foreach (var row in orderRows)
            {
                var orderHeader = headerProfile.Map(row);
                if (orderHeader == null)
                {
                    Console.WriteLine($"Skipping row with missing order header data: {string.Join(", ", row.Values)}");
                    continue;
                }

                var lineItems = lineItemsByOrderId.ContainsKey(orderHeader.OrderId)
                        ? lineItemsByOrderId[orderHeader.OrderId].Select(li => new OrderLineItem
                        {
                            OrderId = orderHeader.OrderId,
                            ProductId = li.ProductId,
                            Quantity = li.Quantity,
                            Description = li.Description ?? string.Empty,
                            Price = li.UnitPrice,
                            Volume = li.Volume?.Select(v => new LineItemVolume
                            {
                                VolumeId = v.VolumeId,
                                Volume = v.Volume
                            }).ToArray()
                        }).ToArray()
                        : null;

                if (orderVolume.TryGetValue(orderHeader.OrderId, out OrderLineItemVolume[]? oVol))
                {
                    lineItems = SetLineItemVolume(lineItems, oVol);
                }

                var shipAddress = !string.IsNullOrWhiteSpace(orderHeader.ShipLine1) ? new ShipAddress
                {
                    Line1 = orderHeader.ShipLine1,
                    Line2 = orderHeader.ShipLine2 ?? "",
                    City = orderHeader.ShipCity ?? "",
                    StateCode = orderHeader.ShipStateCode ?? "",
                    Zip = orderHeader.ShipZip ?? "",
                    CountryCode = orderHeader.ShipCountryCode ?? ""
                } : null;

                orders.Add(new Order
                {
                    Id = orderHeader.OrderId,
                    ShipAddress = shipAddress,
                    CustomerId = orderHeader.CustomerId,
                    Discount = orderHeader.Discount.HasValue ? orderHeader.Discount.Value : 0,
                    OrderDate = orderHeader.OrderDate ?? DateTime.UtcNow,
                    InvoiceDate = orderHeader.InvoiceDate,
                    OrderType = orderHeader.OrderType,
                    Shipping = orderHeader.Shipping ?? 0,
                    Tax = orderHeader.Tax ?? 0,
                    Total = orderHeader.Total ?? 0,
                    Status = orderHeader.Status,
                    Tracking = orderHeader.Tracking,
                    LineItems = lineItems,
                    SubTotal = orderHeader.SubTotal ?? lineItems?.Sum(li => li.Price * li.Quantity) ?? 0,
                    TaxRate = orderHeader.Tax.HasValue && orderHeader.SubTotal.HasValue && orderHeader.SubTotal.Value > 0
                        ? orderHeader.Tax.Value / orderHeader.SubTotal.Value
                        : 0
                });
            }

            List<ErrorItem> ErrorList = new List<ErrorItem>();

            await Parallel.ForEachAsync(orders, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (order, cancellationToken) =>
            {
                try
                {
                    await _orderRepository.InsertOrder(order);
                    Console.WriteLine($"Imported {order.Id}");
                }
                catch (Exception ex)
                {
                    lock (ErrorList) // Ensure thread safety when modifying the shared list
                    {
                        ErrorList.Add(new ErrorItem { Message = ex.Message, Items = [order] });
                    }
                }
            });

            var errors = ErrorList.GroupBy(x => x.Message).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var error in errors)
            {
                Console.WriteLine($"Error during import: {error.Key} count: {error.Value.Count}");
            }

            Console.WriteLine($"Imported {orderRows.Count} rows");
        }

        private OrderLineItem[]? SetLineItemVolume(OrderLineItem[]? current, OrderLineItemVolume[]? volume)
        {
            if (volume == null) return current;

            // If there are no current items, create one item and attach the volume to it
            if (current == null || current.Length == 0)
            {
                return new[]
                {
                    new OrderLineItem
                    {
                        Volume = volume
                            .Select(v => new LineItemVolume
                            {
                                VolumeId = v.VolumeId,
                                Volume = v.Volume
                            })
                            .ToArray()
                    }
                };
            }

            var first = current[0];

            var existingVolumes = first.Volume ?? Array.Empty<LineItemVolume>();

            // Start with existing values from the first item
            var merged = existingVolumes
                .ToDictionary(v => v.VolumeId, v => v, StringComparer.OrdinalIgnoreCase);

            // Replace existing entries or add new ones from incoming volume
            foreach (var v in volume)
            {
                merged[v.VolumeId] = new LineItemVolume
                {
                    VolumeId = v.VolumeId,
                    Volume = v.Volume
                };
            }

            first.Volume = merged.Values.ToArray();

            return current;
        }

        private class ErrorItem
        {
            public string Message { get; set; } = "";
            public Order[] Items { get; set; } = [];
        }
    }
}
