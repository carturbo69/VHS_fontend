namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class UserAddressDto
    {
        public Guid AddressId { get; set; }
        public string ProvinceName { get; set; } = null!;
        public string DistrictName { get; set; } = null!;
        public string WardName { get; set; } = null!;
        public string StreetAddress { get; set; } = null!;
        
        // Tọa độ GPS
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Hiển thị dạng 1 dòng
        public string ToDisplayString()
            => $"{StreetAddress}, {WardName}, {DistrictName}, {ProvinceName}";
    }
}
