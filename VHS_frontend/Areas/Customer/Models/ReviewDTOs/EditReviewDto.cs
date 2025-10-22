using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.ReviewDTOs
{
    public class EditReviewDto
    {
        [Required]
        public Guid ReviewId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public List<string> ExistingImages { get; set; } = new();
        public List<string> RemoveImages { get; set; } = new();
        public IFormFileCollection? NewImages { get; set; }
    }
}
