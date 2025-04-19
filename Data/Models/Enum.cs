namespace ThumbsUpGroceries_backend.Data.Models
{
    public enum PriceUnitType
    {
        ea,
        kg
    }

    public enum TrolleyMethod
    {
        delivery,
        pickup
    }

    public enum OrderStatus
    {
        registered,
        onDelivery,
        completed,
        canceled
    }
}
