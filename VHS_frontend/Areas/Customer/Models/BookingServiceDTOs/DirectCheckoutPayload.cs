namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class DirectCheckoutPayload
    {
        public Guid ServiceId { get; init; }
        public List<Guid> OptionIds { get; init; } = new();
        public Dictionary<Guid, string>? OptionValues { get; init; } // Giá trị textarea user đã nhập

        public DirectCheckoutPayload(Guid serviceId, List<Guid> optionIds, Dictionary<Guid, string>? optionValues = null)
        {
            ServiceId = serviceId;
            OptionIds = optionIds ?? new();
            OptionValues = optionValues;
        }
    }
}
