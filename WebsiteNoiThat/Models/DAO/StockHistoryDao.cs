using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace Models.DAO
{
    public class StockHistoryDao
    {
        DBNoiThat db = null;

        public StockHistoryDao()
        {
            db = new DBNoiThat();
        }

        /// <summary>
        /// Lấy lịch sử giao dịch (nhập, xuất, kiểm kê) của một biến thể tại một kho cụ thể.
        /// </summary>
        /// <param name="warehouseId">ID của Kho</param>
        /// <param name="variantId">ID của Biến thể Sản phẩm</param>
        /// <returns>Danh sách các bản ghi StockHistory</returns>
        public List<StockHistory> GetTransactionHistory(int warehouseId, int variantId)
        {
            try
            {
                var history = db.StockHistories
                                .Include(h => h.Warehouse)
                                .Include(h => h.ProductVariant)
                                .Include(h => h.UserCreated)
                                .Where(h => h.WarehouseId == warehouseId && h.VariantId == variantId)
                                .OrderByDescending(h => h.CreatedAt)
                                .ToList();
                return history;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                Console.WriteLine(ex.Message);
                return new List<StockHistory>();
            }
        }

        /// <summary>
        /// Ghi lại một bản ghi lịch sử giao dịch mới.
        /// (Hàm này sẽ được gọi nội bộ từ StockInDao, StockOutDao, StockCheckDao khi duyệt phiếu)
        /// </summary>
        public bool CreateHistory(StockHistory entity)
        {
            try
            {
                entity.CreatedAt = DateTime.Now;
                db.StockHistories.Add(entity);
                db.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}