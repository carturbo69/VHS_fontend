namespace VHS_frontend.Areas.Admin.Models.Option
{
    public class OptionDTO
    {
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public Guid? TagId { get; set; }
        public string Type { get; set; } = string.Empty; // enum: checkbox, radio, text, optional, etc.
        public Guid? Family { get; set; } // For radio buttons: if one is selected, others are hidden
    }

    public class OptionCreateDTO
    {
        public string OptionName { get; set; } = string.Empty;
        public Guid? TagId { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid? Family { get; set; }
    }

    public class OptionUpdateDTO
    {
        public string OptionName { get; set; } = string.Empty;
        public Guid? TagId { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid? Family { get; set; }
    }
}

