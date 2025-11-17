using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Option
{
    public class OptionCreateDTO
    {
        [Required(ErrorMessage = "Tên option không được để trống.")]
        public string OptionName { get; set; } = string.Empty;

        public Guid? TagId { get; set; }

        [Required(ErrorMessage = "Loại option không được để trống (checkbox, radio, text, optional, etc.).")]
        public string Type { get; set; } = string.Empty; // enum: checkbox, radio, text, optional, etc.

        public Guid? Family { get; set; } // For radio buttons: if one is selected, others are hidden
    }
}