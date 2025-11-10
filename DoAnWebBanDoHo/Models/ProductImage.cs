namespace DoAnWebBanDoHo.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        // Đường dẫn đến file ảnh (vd: /images/gallery/ten-file.jpg)
        public string ImageUrl { get; set; }

        // Khóa ngoại liên kết đến sản phẩm
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
    }
}