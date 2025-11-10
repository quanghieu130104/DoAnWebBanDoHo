using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần cho DisplayName

namespace DoAnWebBanDoHo.Models // Đảm bảo namespace này khớp với project của bạn
{
    public class ProductSale
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalQuantitySold { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalRevenue { get; set; }
    }

    public class SalesStatisticsViewModel
    {
        [Display(Name = "Tổng Doanh Thu")]
        public decimal TotalOverallRevenue { get; set; }

        [Display(Name = "Tổng Số Đơn Hàng")]
        public int TotalOrders { get; set; }

        [Display(Name = "Tổng Số Sản Phẩm Đã Bán")]
        public int TotalItemsSold { get; set; }

        [Display(Name = "Sản Phẩm Bán Chạy Nhất")]
        public List<ProductSale> TopSellingProducts { get; set; } = new List<ProductSale>();

        [Display(Name = "Doanh Thu Theo Tháng")]
        public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new Dictionary<string, decimal>();

        [Display(Name = "Doanh Thu Theo Năm")]
        public Dictionary<string, decimal> YearlyRevenue { get; set; } = new Dictionary<string, decimal>();
    }
}
