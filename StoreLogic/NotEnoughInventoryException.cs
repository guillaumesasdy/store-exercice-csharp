using System;
using System.Collections.Generic;

namespace StoreLogic
{
    /// <summary>
    /// The exception contains the list of books that can't fulfill a basket order,
    /// with the missing quantities for each book.
    /// </summary>
    public class NotEnoughInventoryException : Exception
    {
        public IEnumerable<INameQuantity> Missing { get; }

        public NotEnoughInventoryException(IEnumerable<INameQuantity> missing)
        {
            Missing = missing;
        }
    }
}
