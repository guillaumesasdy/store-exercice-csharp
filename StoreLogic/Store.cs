using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
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
            throw new NotImplementedException();
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
