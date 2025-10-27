namespace VHS_frontend.Areas.Customer.Models.ReviewDTOs
{
    public class ReviewListItemDto
    {
    
        public Guid ReviewId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid UserId { get; set; }

        public string FullName { get; set; }
        public string UserAvatarUrl { get; set; }
        public int? Rating { get; set; }
        public string Comment { get; set; }
        public string Reply { get; set; }
        public string ServiceTitle { get; set; }
        public string ServiceThumbnailUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int LikeCount { get; set; }

        public int EditCount { get; set; }
        // Không còn set cứng; tự tính: có Reply => không cho sửa/xoá
        public bool CanEdit { get; set; }                // <- server set
        public bool CanDelete { get; set; }              // <- server set


        // 👇 MỚI: danh sách ảnh của đánh giá (tối đa 5 ảnh sẽ hiển thị)
        public List<string> ReviewImageUrls { get; set; } = new();
    }
}

