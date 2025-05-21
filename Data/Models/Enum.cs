namespace ThumbsUpGroceries_backend.Data.Models
{
    public enum PriceUnitType
    {
        ea,
        g
    }

    public enum TrolleyMethod
    {
        delivery,
        pickup
    }

    public enum TrolleyTimeSlotStatus
    {
        available,
        unavailable
    }

    public enum OrderStatus
    {
        registered,
        onDelivery,
        completed,
        canceling,
        canceled
    }

    public enum MembershipStatus
    {
        active,
        pastDue,
        canceled,
    }
}
