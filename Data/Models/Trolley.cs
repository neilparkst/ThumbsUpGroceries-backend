using System.ComponentModel.DataAnnotations;

namespace ThumbsUpGroceries_backend.Data.Models
{
    public static class TrolleyConstants
    {
        public const int DELIVERY_FEE = 869;
        public const int BAG_FEE = 149;
    }

    public class Trolley
    {
        public int TrolleyId { get; set; }
        public Guid UserId { get; set; }
        public int ItemCount { get; set; }
        public TrolleyMethod Method { get; set; } = TrolleyMethod.pickup;
    }

    public class TrolleyItem
    {
        public int TrolleyItemId { get; set; }
        public int TrolleyId { get; set; }
        public int ProductId { get; set; }
        public PriceUnitType PriceUnitType { get; set; } = PriceUnitType.ea;
        public int Quantity { get; set; }
    }

    public class TrolleyCountResponse
    {
        public int TrolleyId { get; set; }
        public int ItemCount { get; set; }
    }

    public class TrolleyItemRequest
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public PriceUnitType PriceUnitType { get; set; }
        [Required]
        public int Quantity { get; set; }
    }

    public class TrolleyItemResponse
    {
        public int TrolleyItemId { get; set; }
        public int ProductId { get; set; }
        public PriceUnitType PriceUnitType { get; set; } = PriceUnitType.ea;
        public int Quantity { get; set; }
    }

    public class TrolleyItemDeleteResponse
    {
        public int TrolleyItemId { get; set; }
        public int ProductId { get; set; }
    }

    public class TrolleyItemMany
    {
        public int TrolleyItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int ProductPrice { get; set; }
        public PriceUnitType ProductPriceUnitType { get; set; } = PriceUnitType.ea;
        public string Image { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int TotalPrice { get; set; }
    }

    public class TrolleyContentResponse
    {
        public int TrolleyId { get; set; }
        public int ItemCount { get; set; }
        public List<TrolleyItemMany> Items { get; set; } = new List<TrolleyItemMany>();
        public int SubTotalPrice { get; set; }
        public TrolleyMethod Method { get; set; } = TrolleyMethod.pickup;
        public int ServiceFee { get; set; }
        public int BagFee { get; set; }
        public int TotalPrice { get; set; }
    }

    public class TrolleyItemForValidationRequest
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int ProductPrice { get; set; }
        [Required]
        public PriceUnitType PriceUnitType { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public int TotalPrice { get; set; }
    }

    public class TrolleyValidationRequest
    {
        [Required]
        public int TrolleyId { get; set; }
        [Required]
        public List<TrolleyItemForValidationRequest> Items { get; set; }
        [Required]
        public int SubTotalPrice { get; set; }
        [Required]
        public TrolleyMethod Method { get; set; }
        [Required]
        public int ServiceFee { get; set; }
        [Required]
        public int BagFee { get; set; }
        [Required]
        public int TotalPrice { get; set; }
    }

    public class TrolleyValidationResponse
    {
        public bool IsValid { get; set; }
    }

    public class TrolleyTimeSlot
    {
        public int SlotId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TrolleyMethod Method { get; set; }
        public int SlotCount { get; set; }
    }

        public class TrolleyTimeSlotDto
    {
        public int SlotId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TrolleyTimeSlotStatus Status { get; set; } = TrolleyTimeSlotStatus.available;
    }

    public class TrolleyCheckoutSessionRequest
    {
        [Required]
        public int TrolleyId { get; set; }
        [Required]
        public int ChosenTimeSlot { get; set; }
        [Required]
        public int TimeSlotRecordId { get; set; }
        [Required]
        public string ChosenAddress { get; set; }
        [Required]
        public string SuccessUrl { get; set; }
        [Required]
        public string CancelUrl { get; set; }
    }
}