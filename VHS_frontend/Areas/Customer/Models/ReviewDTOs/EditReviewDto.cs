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
        public string? Comment { get; set; }
        public List<IFormFile>? NewImages { get; set; }
        public List<string>? RemoveImages { get; set; }
    }
}
