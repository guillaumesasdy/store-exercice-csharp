namespace StoreLogic
{
    class NameQuantity : INameQuantity
    {
        public string Name { get; set; }

        public int Quantity { get; set; }

        public NameQuantity(
            string name,
            int quantity)
        {
            Name = name;
            Quantity = quantity;
        }
    }
}
