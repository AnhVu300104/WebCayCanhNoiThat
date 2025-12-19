using Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebsiteNoiThat.Models
{
    public class ProductViewModel
    {
        [Key]
        public int? ProductId { get; set; }

        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự")]
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [DisplayName("Tên sản phẩm")]
        public string Name { get; set; }

        [DisplayName("Mô tả sản phẩm")]
        public string Description { get; set; }

        [DisplayName("Đơn giá")]
        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(0, int.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public int? Price { get; set; }

        [DisplayName("Số lượng")]
        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int? Quantity { get; set; }

        //[DisplayName("Nhà cung cấp")]
        //[Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        //public int? ProviderId { get; set; }

        [DisplayName("Danh mục sản phẩm")]
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int? CateId { get; set; }

        [DisplayName("Danh mục sản phẩm")]
        public string CateName { get; set; }

        [DisplayName("Nhà cung cấp")]
        public string ProviderName { get; set; }

        [DisplayName("Ảnh")]
        public string Photo { get; set; }

        [DisplayName("Ngày bắt đầu KM")]
        [Column(TypeName = "date")]
        public DateTime? StartDate { get; set; }

        [DisplayName("Ngày kết thúc KM")]
        [Column(TypeName = "date")]
        public DateTime? EndDate { get; set; }

        [DisplayName("Giảm giá (%)")]
        [Range(0, 100, ErrorMessage = "Giảm giá phải từ 0% đến 100%")]
        public int? Discount { get; set; }

        [DisplayName("Trạng thái")]
        public bool IsActive { get; set; } = true;

        [DisplayName("Trạng thái bán")]
        public string StatusText
        {
            get { return IsActive ? "Còn bán" : "Ngừng bán"; }
        }
    }
}