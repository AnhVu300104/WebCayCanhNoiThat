using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.DAO
{
    public class OrderDetailDao
    {
        DBNoiThat db = null;

        public OrderDetailDao()
        {
            db = new DBNoiThat();
        }

        public bool Insert(OrderDetail orderDetail)
        {
            try
            {
                // 1. Thêm bản ghi vào bảng OrderDetail
                db.OrderDetails.Add(orderDetail);

                // 2. Trừ tồn kho (Quan trọng với CSDL mới)
                if (orderDetail.VariantId.HasValue && orderDetail.VariantId > 0)
                {
                    // Trường hợp 1: Sản phẩm có biến thể (Variant)
                    var variant = db.ProductVariants.Find(orderDetail.VariantId);
                    if (variant != null)
                    {
                        // Trừ tồn kho trong bảng biến thể
                        variant.StockQuantity = (variant.StockQuantity ?? 0) - orderDetail.Quantity;

                        // Đảm bảo không âm kho (tùy nghiệp vụ có thể cho phép âm hoặc không)
                        if (variant.StockQuantity < 0) variant.StockQuantity = 0;
                    }
                }
                else
                {
                    // Trường hợp 2: Sản phẩm đơn (Logic cũ)
                    var product = db.Products.Find(orderDetail.ProductId);
                    if (product != null)
                    {
                        product.Quantity = (product.Quantity ?? 0) - orderDetail.Quantity;
                        if (product.Quantity < 0) product.Quantity = 0;
                    }
                }

                // 3. Lưu thay đổi
                db.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                // Có thể log lỗi ex.Message tại đây để debug
                return false;
            }
        }
    }
}