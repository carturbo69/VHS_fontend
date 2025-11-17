namespace VHS_frontend.Areas.Provider.Models.Service
{
    public class OptionDTO
    {
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public Guid? TagId { get; set; }
        public string Type { get; set; } = string.Empty; // enum: checkbox, radio, text, optional, etc.
        public Guid? Family { get; set; } // For radio buttons: if one is selected, others are hidden
        public string? Value { get; set; } // Stores the value if any
    }
}

