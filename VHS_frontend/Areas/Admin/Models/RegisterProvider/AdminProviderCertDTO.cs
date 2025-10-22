namespace VHS_frontend.Areas.Admin.Models.RegisterProvider
{
    public class AdminProviderCertDTO
    {
        public Guid CertificateId { get; set; }
        public Guid CategoryId { get; set; }
        public string? Description { get; set; }

        // JSON string: ["url1","url2",...]
        public string? Images { get; set; }
    }
}
