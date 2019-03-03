using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StoreLogic
{
    public class Store : IStore
    {
        private string _jsonFormat = string.Empty;
        private string JsonFormat
        {
            get
            {
                if (string.IsNullOrEmpty(_jsonFormat))
                {
                    // TODO GSAS Move the hardcoded filepath below to a config file 
                    _jsonFormat = File.ReadAllText("format.json");
                }
                return _jsonFormat;
            }
        }

        private JSchema _storeSchema = null;
        private JSchema StoreSchema
        {
            get
            {
                if (_storeSchema == null)
                {
                    _storeSchema = JSchema.Parse(JsonFormat);
                }
                return _storeSchema;
            }
        }

        private JObject StoreData { get; set; } = null;

        /// <summary>
        /// Returns the basket's price with discount if necessary, rounded to the nearest hundredth.
        /// </summary>
        /// <param name="basketByNames">A list of all the books' name to price.</param>
        /// <returns>The basket's price, or zero if the basket is empty or the store has not been initialized.</returns>
        public double Buy(params string[] basketByNames)
        {
            double price = 0;

            if (StoreData != null
                && basketByNames != null
                && basketByNames.Length > 0)
            {
                var basket = CountBooks(basketByNames);
                CheckInventoryQuantities(basket);
                double total = CalculatePrice(basket);
                double discount = CalculateDiscount(basket);
                price = total - discount;
            }

            return price;
        }

        /// <summary>
        /// Returns the quantities of books in the given basket.
        /// </summary>
        /// <param name="basketByNames"></param>
        /// <returns></returns>
        private IEnumerable<INameQuantity> CountBooks(string[] basketByNames)
        {
            return basketByNames.GroupBy(book => book)
                .Select(g => new NameQuantity(g.Key, g.Count()));
        }

        /// <summary>
        /// Check that each book's quantity in the basket can be provided by the store.
        /// </summary>
        /// <param name="basket">List of books.</param>
        /// <exception cref="NotEnoughInventoryException">The store (inventory) can't fulfill the order.</exception>
        private void CheckInventoryQuantities(IEnumerable<INameQuantity> basket)
        {
            JArray inventory = (JArray)StoreData["Catalog"];
            var missing = new List<NameQuantity>();

            foreach(var fromBasket in basket)
            {
                var inInventory = inventory.FirstOrDefault(book =>
                    book.Value<string>("Name") == fromBasket.Name);

                if (inInventory == null)
                {
                    missing.Add(new NameQuantity(
                        fromBasket.Name, 
                        fromBasket.Quantity));
                }
                else
                {
                    int maxQuantity = inInventory.Value<int>("Quantity");

                    if (fromBasket.Quantity > maxQuantity)
                    {
                        missing.Add(new NameQuantity(
                            fromBasket.Name, 
                            fromBasket.Quantity - maxQuantity));
                    }
                }
            }

            if (missing.Count > 0)
            {
                throw new NotEnoughInventoryException(missing);
            }
        }

        /// <summary>
        /// Returns the price of the basket.
        /// </summary>
        /// <param name="basket"></param>
        /// <returns></returns>
        private double CalculatePrice(IEnumerable<INameQuantity> basket)
        {
            double price = 0;

            foreach (var book in basket)
            {
                var fromInventory = StoreData["Catalog"].First(b => b.Value<string>("Name") == book.Name);
                price += Math.Round(book.Quantity * fromInventory.Value<double>("Price"), 2);
            }

            return price;
        }

        /// <summary>
        /// Returns the discount for the basket.
        /// </summary>
        /// <param name="basket"></param>
        /// <returns></returns>
        private double CalculateDiscount(IEnumerable<INameQuantity> basket)
        {
            double amount = 0;
            var booksCategories = GetBooksCategories(basket);
            var discountedCategories = GetDiscountedCategories(basket, booksCategories);

            // Apply the discount for the first book which belongs to a discounted category
            foreach(var discounted in booksCategories.Where(b => discountedCategories.Contains(b.Value)))
            {
                var book = StoreData["Catalog"].First(b => b.Value<string>("Name") == discounted.Key);
                var category = StoreData["Category"].First(c => c.Value<string>("Name") == discounted.Value);

                double unitaryPrice = book.Value<double>("Price");
                double discount = category.Value<double>("Discount");
                amount += Math.Round(unitaryPrice * discount, 2);
            }

            return amount;
        }

        /// <summary>
        /// Returns a dictionnary filled with the category for each book in the basket.
        /// </summary>
        /// <param name="basket"></param>
        /// <returns></returns>
        private IDictionary<string, string> GetBooksCategories(IEnumerable<INameQuantity> basket)
        {
            var booksCategories = new Dictionary<string, string>();

            foreach(var book in basket)
            {
                var fromInventory = StoreData["Catalog"].First(b => b.Value<string>("Name") == book.Name);
                string category = fromInventory.Value<string>("Category");
                booksCategories.Add(book.Name, category);
            }

            return booksCategories;
        }

        /// <summary>
        /// Returns discounted categories. That is to say categories with one or more book in the basket.
        /// </summary>
        /// <param name="basket"></param>
        /// <param name="booksCategories">A dictionnary of book name (key), its category (value).</param>
        /// <returns></returns>
        private IList<string> GetDiscountedCategories(
            IEnumerable<INameQuantity> basket,
            IDictionary<string, string> booksCategories)
        {
            var categoriesQuantities = from book in basket
                                       join bookCategory in booksCategories on book.Name equals bookCategory.Key
                                       select new { Category = bookCategory.Value, book.Quantity };
            
            return categoriesQuantities.GroupBy(
                b => b.Category,
                b => b.Quantity,
                (category, quantity) => new
                {
                    Category = category,
                    Sum = quantity.Sum()
                })
                .Where(g => g.Sum > 1)
                .Select(g => g.Category)
                .ToList();
        }

        /// <summary>
        /// Import the catalog into the store.
        /// </summary>
        /// <param name="catalogAsJson">JSON data for the catalog.</param>
        /// <exception cref="ArgumentException">The JSON data doesn't validate against our schema.</exception>
        public void Import(string catalogAsJson)
        {
            StoreData = JObject.Parse(catalogAsJson);
            if(! StoreData.IsValid(StoreSchema))
            {
                StoreData = null;
                throw new ArgumentException("The catalog data doesn't validate against our schema.");
            }
        }

        /// <summary>
        /// Returns the quantity of the given book.
        /// </summary>
        /// <param name="name">The book's name in the catalog.</param>
        /// <returns>The book's quantity, or zero if the store has not been initialized.</returns>
        public int Quantity(string name)
        {
            int quantity = 0;

            if (StoreData != null)
            {
                JArray books = (JArray)StoreData["Catalog"];
                var book = books.FirstOrDefault(b => b.Value<string>("Name") == name);
                if (book != null)
                {
                    quantity = book.Value<int>("Quantity");
                }
            }

            return quantity;
        }
    }
}
