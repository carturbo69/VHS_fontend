namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class UserAddressDto
    {
        public Guid AddressId { get; set; }
        public string ProvinceName { get; set; } = null!;
        public string DistrictName { get; set; } = null!;
        public string WardName { get; set; } = null!;
        public string StreetAddress { get; set; } = null!;
        
        public string? RecipientName { get; set; }
        
        public string? RecipientPhone { get; set; }
        
        // Tọa độ GPS
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Hiển thị dạng 1 dòng - loại bỏ dấu phẩy dư
        public string ToDisplayString()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(StreetAddress))
                parts.Add(StreetAddress.Trim());
            
            if (!string.IsNullOrWhiteSpace(WardName))
                parts.Add(WardName.Trim());
            
            if (!string.IsNullOrWhiteSpace(DistrictName))
                parts.Add(DistrictName.Trim());
            
            if (!string.IsNullOrWhiteSpace(ProvinceName))
                parts.Add(ProvinceName.Trim());
            
            return string.Join(", ", parts);
        }
    }
}
