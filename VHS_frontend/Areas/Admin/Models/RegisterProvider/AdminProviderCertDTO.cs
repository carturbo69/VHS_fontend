namespace VHS_frontend.Areas.Admin.Models.RegisterProvider
{
    public class AdminProviderCertDTO
    {
        public Guid CertificateId { get; set; }
        public Guid CategoryId { get; set; }
        public string? Description { get; set; }
        /// <summary>
        /// JSON string (array of urls) trả từ API.
        /// FE có thể deserialize sang List&lt;string&gt; nếu cần.
        /// </summary>
        public string? Images { get; set; }
    }
}
