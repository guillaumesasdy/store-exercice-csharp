using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StoreLogic.Tests
{
    [TestClass]
    public class StoreTests
    {
        private IStore _store = null;

        [TestInitialize]
        public void Initialize()
        {
            _store = new Store();
            string data = File.ReadAllText("data.json");
            _store.Import(data);
        }

        [TestMethod]
        public void Quantity_MissingBook_ReturnsZero()
        {
            int qtyBook = _store.Quantity("Unknown author - Missing book");
            Assert.AreEqual(0, qtyBook);
        }

        [TestMethod]
        public void Quantity_ExistingBook_ReturnsQuantity()
        {
            int qtyBook = _store.Quantity("Ayn Rand - FountainHead");
            Assert.AreEqual(10, qtyBook);
        }

        [TestMethod]
        public void Buy_SingleBook_CatalogPrice()
        {
            double price = _store.Buy(
                "Robin Hobb - Assassin Apprentice");
            Assert.AreEqual(12, price);
        }

        [TestMethod]
        public void Buy_TwoBooksFromDifferentCategories_CatalogPrice()
        {
            double price = _store.Buy(
                "Robin Hobb - Assassin Apprentice",
                "Isaac Asimov - Robot series");
            Assert.AreEqual(17, price);
        }
        
        [TestMethod]
        public void Buy_Example1()
        {
            double price = _store.Buy(
                "J.K Rowling - Goblet Of fire",
                "Robin Hobb - Assassin Apprentice",
                "Robin Hobb - Assassin Apprentice");
            Assert.AreEqual(30, price);
        }

        [TestMethod]
        public void Buy_Example2()
        {
            double price = _store.Buy(
                "Ayn Rand - FountainHead",
                "Isaac Asimov - Foundation",
                "Isaac Asimov - Robot series",
                "J.K Rowling - Goblet Of fire",
                "J.K Rowling - Goblet Of fire",
                "Robin Hobb - Assassin Apprentice",
                "Robin Hobb - Assassin Apprentice");
            Assert.AreEqual(69.95, price);
        }
        
        [TestMethod]
        public void Buy_NotEnoughQuantity_ThrowException()
        {
            NotEnoughInventoryException expectedExcetpion = null;

            try
            {
                double price = _store.Buy(
                    "Isaac Asimov - Robot series",
                    "Isaac Asimov - Foundation",
                    "Isaac Asimov - Foundation",
                    "Isaac Asimov - Foundation",
                    "J.K Rowling - Goblet Of fire",
                    "J.K Rowling - Goblet Of fire",
                    "J.K Rowling - Goblet Of fire");
            }
            catch(NotEnoughInventoryException exception)
            {
                expectedExcetpion = exception;
            }
            Assert.IsNotNull(expectedExcetpion);

            List<INameQuantity> missing = expectedExcetpion.Missing.ToList();
            Assert.AreEqual(2, missing.Count);

            INameQuantity asimov = missing.SingleOrDefault(e => e.Name == "Isaac Asimov - Foundation");
            Assert.IsNotNull(asimov);
            Assert.AreEqual(2, asimov.Quantity);

            INameQuantity rowling = missing.SingleOrDefault(e => e.Name == "J.K Rowling - Goblet Of fire");
            Assert.IsNotNull(rowling);
            Assert.AreEqual(1, rowling.Quantity);
        }
    }
}
