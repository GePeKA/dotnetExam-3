namespace grpcServer.Entities
{
    public class Item
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public double Price { get; set; }
    }
}
