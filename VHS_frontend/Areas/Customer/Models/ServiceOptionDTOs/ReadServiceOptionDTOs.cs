namespace VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs
{
    public class ReadServiceOptionDTOs
    {
        public Guid ServiceOptionId { get; set; }
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = null!;
        public Guid? TagId { get; set; }
        public string Type { get; set; } = null!; // enum: checkbox, radio, text, optional, etc.
        public Guid? Family { get; set; } // For radio buttons: if one is selected, others are hidden
        public string? Value { get; set; } // Stores the value for the option
    }
}
