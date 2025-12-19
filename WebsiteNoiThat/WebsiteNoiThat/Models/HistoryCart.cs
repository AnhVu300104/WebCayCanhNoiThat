namespace WebsiteNoiThat.Models
{
    using System;

    public class HistoryCart
    {
        public int OrderId { get; set; }
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }

        public string Name { get; set; }
        public string Photo { get; set; }

        // --- Các thuộc tính mới ---
        public string ImageVariant { get; set; }
        public string VariantInfo { get; set; }

        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public int? Discount { get; set; }

        // --- THÊM DÒNG NÀY ĐỂ SỬA LỖI ---
        public int StatusId { get; set; } // Dùng để kiểm tra trạng thái (VD: 1=Mới, 3=Đã hủy)

        public string NameStatus { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}