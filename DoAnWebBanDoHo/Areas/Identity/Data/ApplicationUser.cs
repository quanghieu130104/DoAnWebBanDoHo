using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

// Đảm bảo namespace này khớp với vị trí file của bạn
namespace DoAnWebBanDoHo.Areas.Identity.Data
{
    // Lớp này PHẢI kế thừa từ IdentityUser
    public class ApplicationUser : IdentityUser
    {
        // Thêm các thuộc tính tùy chỉnh mà bạn muốn
        // (Đây là các thuộc tính bạn đã dùng trong Program.cs)

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}