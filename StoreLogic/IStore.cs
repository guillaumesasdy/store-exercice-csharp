using System;

namespace StoreLogic
{
    public interface IStore
    {
        void Import(string catalogAsJson);

        int Quantity(string name);

        double Buy(params String[] basketByNames);
    }
}
