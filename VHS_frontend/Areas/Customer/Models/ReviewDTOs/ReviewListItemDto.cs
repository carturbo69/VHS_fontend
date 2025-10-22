namespace VHS_frontend.Areas.Customer.Models.ReviewDTOs
{
    public class ReviewListItemDto
    {
        //public Guid ReviewId { get; set; }
        //public Guid ServiceId { get; set; }
        //public Guid UserId { get; set; }

        //public int? Rating { get; set; }              // 1..5
        //public string? Comment { get; set; }          // nội dung người mua
        //public string? Images { get; set; }           // URL ảnh do user đính kèm (nếu có, có thể là JSON/CSV)
        //public string? Reply { get; set; }            // phản hồi của người bán
        //public DateTime? CreatedAt { get; set; }
        //public bool? IsDeleted { get; set; }

        //// Từ User
        //public string? FullName { get; set; }
        //public string? UserAvatarUrl { get; set; }

        //// Từ Service
        //public string? ServiceTitle { get; set; }
        //public string? ServiceThumbnailUrl { get; set; }

        //// Phụ trợ UI
        //public int LikeCount { get; set; } = 0;
        //public bool CanEdit { get; set; } = true;     // để bật/tắt nút Sửa/Xóa theo điều kiện
        //public bool CanDelete { get; set; } = true;

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

        // Không còn set cứng; tự tính: có Reply => không cho sửa/xoá
        public bool CanEdit => string.IsNullOrWhiteSpace(Reply);
        public bool CanDelete => string.IsNullOrWhiteSpace(Reply);

        // 👇 MỚI: danh sách ảnh của đánh giá (tối đa 5 ảnh sẽ hiển thị)
        public List<string> ReviewImageUrls { get; set; } = new();
    }
}

