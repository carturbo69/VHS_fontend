namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class DirectCheckoutPayload
    {
        public Guid ServiceId { get; init; }
        public List<Guid> OptionIds { get; init; } = new();

        public DirectCheckoutPayload(Guid serviceId, List<Guid> optionIds)
        {
            ServiceId = serviceId;
            OptionIds = optionIds ?? new();
        }
    }
}
