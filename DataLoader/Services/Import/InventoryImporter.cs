using DataLoader.Repositories;
using DataLoader.Repositories.Models;
using System.Globalization;
using System.Text;

namespace DataLoader.Services.Import
{
    internal class InventoryImporter
    {
        private readonly ProductRepository _productRepository;
        private readonly CSVFileReader _csvFileReader;

        public InventoryImporter(CSVFileReader sVFileReader, ProductRepository productRepository)
        {
            _productRepository = productRepository;
            _csvFileReader = sVFileReader;
        }

        public async Task ImportInventory(string productFile, string priceFile)
        {
            var productIds = await ImportProducts(productFile);
            await ImportPrices(productIds, priceFile);
        }

        public static List<string> ParseFile(string filePath)
        {
            var result = new List<string>();
            var buffer = new StringBuilder(); // Buffer to handle multi-line values
            string? line;

            using (var reader = new StreamReader(filePath))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    buffer.AppendLine(line);

                    // Check if the line contains the separator '|', meaning it's a full record
                    if (line.Contains("|"))
                    {
                        var fullLine = buffer.ToString().TrimEnd(); // Get the accumulated content
                        var values = fullLine.Split('|'); // Split by '|'
                        result.Add(values.First());
                        buffer.Clear(); // Reset buffer for the next record
                    }
                }

                // Handle any remaining buffer content
                if (buffer.Length > 0)
                {
                    var values = buffer.ToString().TrimEnd().Split('|');
                    result.Add(values.First());
                }
            }

            return result;
        }

        public async Task<string[]> ImportProducts(string productFile)
        {
            await Task.CompletedTask;
            var descriptionRows = ParseFile(@"C:\Users\leona\Downloads\Common Sense Inventory Product Descriptions Only (2-20-25).csv");
            var productRows = _csvFileReader.ReadCsvFile(@"C:\Users\leona\Downloads\Common Sense Inventory Products No Description (2-12-25).csv");


            var products = productRows.Select(x =>
            {
                bool.TryParse(x["enabled"], out bool enabled);
                bool.TryParse(x["requiresShipping"], out bool requiresShipping);

                string? imageUrl = x["imageUrl"];
                if (imageUrl == "https://commonsense.corpadmin.directscale.com/CMS/Images/Inventory") imageUrl = null;

                return new Product
                {
                    Id = x["itemId"],
                    Name = x["name"],
                    //Description = x["description"],
                    Specifications = x["specifications"],
                    ImageUrl = imageUrl,
                    CategoryIds = new[] { x["categoryIds"] },
                    UPC = x["upc"],
                    MPN = x["mpn"],
                    HSCode = x["hsCode"],
                    Enabled = enabled,
                    OutOfStockStatus = OutOfStockStatus.InStock,
                    TaxClassId = x["taxClassId"],
                    ProductClass = ProductClass.Standard
                };
            }).ToArray();

            var descriptions = descriptionRows.Select(x =>
            {
                var comma = x.IndexOf(',');
                var id = x.Substring(0, comma);
                var desc = x.Substring(comma + 1).Trim().Trim(',').Trim('"');

                return new Product
                {
                    Id = id,
                    Description = desc
                };
            }).ToDictionary(x => x.Id, y => y);

            foreach (var product in products)
            {
                if (descriptions.TryGetValue(product.Id, out var descriptionRow))
                {
                    product.Description = descriptionRow.Description;
                }
            }

            products = products.GroupBy(x => x.Name).SelectMany(g =>
            {
                List<Product> items = new List<Product>();

                foreach(var group in g.GroupBy(t => t.ImageUrl).OrderByDescending(x => x.Key))
                {
                    var item = group.First();
                    if (string.IsNullOrEmpty(item.ImageUrl))
                    {
                        if (items.Count() == 0) items.Add(item);
                    }
                    else
                    {
                        items.Add(item);
                    }
                }

                return items;
            }).ToArray();

            List<object> ErrorList = new List<object>();
            List<string> results = new List<string>();

            

            foreach (var product in products)
            {
                try
                {
                    await _productRepository.InsertProduct(product);
                    Console.WriteLine($"Imported {product.Id}");
                    results.Add(product.Id);
                }
                catch (Exception ex)
                {
                    ErrorList.Add(new { msg = ex.Message, product = product });
                }
            }

            string aa = string.Empty;
            foreach (var item in ErrorList)
            {
                aa += "\r\n" + item;
            }

            Console.WriteLine($"Imported {products.Length} rows");
            return results.ToArray();
        }

        public async Task ImportPrices(string[] productIds, string priceFile)
        {
            var timeZoneId = "Mountain Standard Time";
            var dateFormat = "M/d/yyyy H:mm";

            await Task.CompletedTask;
            var priceRows = _csvFileReader.ReadCsvFile(@"C:\Users\leona\Downloads\Common Sense Inventory Prices (2-12-25).csv");
            var idHash = productIds.ToHashSet();

            var prices = priceRows.Select(x =>
            {
                var start = ReadDate(x["start"], dateFormat, timeZoneId);
                var end = ReadDate(x["end"], dateFormat, timeZoneId);
                decimal.TryParse(x["CV"], out decimal cv);
                decimal.TryParse(x["QV"], out decimal qv);
                decimal.TryParse(x["price"], out decimal price);

                if (end.HasValue && end.Value.Year == 9999) end = null;
                var orderTypeId = x["orderTypeIds"];
                if (orderTypeId == "1") orderTypeId = "standard";
                if (orderTypeId == "2") orderTypeId = "autoship";
                if (string.IsNullOrWhiteSpace(orderTypeId)) orderTypeId = "standard";

                return new ProductPrice
                {
                    Id = x["id"],
                    ProductId = x["productId"],
                    Price = price,
                    PriceCurrency = x["priceCurrency"],
                    Volume = new[]{
                        new PriceVolume { VolumeId = "CV", Volume = cv },
                        new PriceVolume { VolumeId = "QV", Volume = qv },
                    },
                    Start = start.HasValue ? start.Value : DateTime.UtcNow,
                    End = end,
                    PriceType = PriceType.Price,
                    StoreIds = new[] { x["storeIds"] },
                    PriceGroups = new[] { x["priceGroups"] },
                    RegionIds = new[] { x["regionIds"] },
                    OrderTypeIds = new[] { orderTypeId },
                };
            }).ToArray();


            prices = prices.GroupBy(x => new { x.Id, x.ProductId }).Select(x =>
            {
                var res = x.First();
                res.StoreIds = x.SelectMany(y => y.StoreIds).Distinct().ToArray();
                res.PriceGroups = x.SelectMany(y => y.PriceGroups).Distinct().ToArray();
                res.RegionIds = x.SelectMany(y => y.RegionIds).Distinct().ToArray();
                res.OrderTypeIds = x.SelectMany(y => y.OrderTypeIds).Distinct().ToArray();

                return res;
            }).ToArray();

            List<object> ErrorList = new List<object>();

            foreach (var price in prices)
            {
                try
                {
                    if (idHash.Contains(price.ProductId))
                    {
                        await _productRepository.InsertPrice(price);
                        Console.WriteLine($"Imported {price.Id}");
                    }
                    else
                    {
                        ErrorList.Add(new { msg = "Missing Product Id", price = price });
                    }
                }
                catch (Exception ex)
                {
                    ErrorList.Add(new { msg = ex.Message, price = price });
                }
            }

            string aa = string.Empty;
            foreach (var item in ErrorList)
            {
                aa += "\r\n" + item;
            }

            Console.WriteLine($"Imported {prices.Length} rows");
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
