using Models.EF;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.DAO
{
    public class WarehouseDao
    {
        DBNoiThat db = null;

        public WarehouseDao()
        {
            db = new DBNoiThat();
        }

        // =============================================
        // 1. QUẢN LÝ KHO (WAREHOUSE CRUD)
        // =============================================

        /// <summary>
        /// Lấy danh sách tất cả các kho đang hoạt động
        /// </summary>
        public List<Warehouse> ListAll()
        {
            return db.Warehouses.Where(x => x.IsActive == true || x.IsActive== false).OrderBy(x => x.Name).ToList();
        }

        public Warehouse ViewDetail(int id)
        {
            return db.Warehouses.Find(id);
        }

        public int Insert(Warehouse entity)
        {
            try
            {
                if (db.Warehouses.Any(x => x.Code == entity.Code)) return -1; // Trùng mã

                entity.CreatedAt = DateTime.Now;
                entity.IsActive = true;
                db.Warehouses.Add(entity);
                db.SaveChanges();
                return entity.WarehouseId;
            }
            catch
            {
                return 0;
            }
        }

        public bool Update(Warehouse entity)
        {
            try
            {
                var model = db.Warehouses.Find(entity.WarehouseId);
                if (model != null)
                {
                    model.Name = entity.Name;
                    model.Address = entity.Address;
                    model.Phone = entity.Phone;
                    model.ManagerName = entity.ManagerName;
                    model.ManagerPhone = entity.ManagerPhone;
                    model.Type = entity.Type;
                    model.IsActive = entity.IsActive;
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool Delete(int id)
        {
            try
            {
                var model = db.Warehouses.Find(id);
                if (model != null)
                {
                    // Soft delete (Chỉ ẩn đi chứ không xóa thật để giữ lịch sử)
                    model.IsActive = false;
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // =============================================
        // 2. QUẢN LÝ TỒN KHO (INVENTORY)
        // =============================================

        /// <summary>
        /// Lấy danh sách tồn kho của một kho cụ thể
        /// </summary>
        public List<WarehouseStock> GetStockByWarehouse(int warehouseId)
        {
            return db.WarehouseStocks
                     .Include("ProductVariant")
                     .Include("ProductVariant.Product")
                     .Where(x => x.WarehouseId == warehouseId)
                     .ToList();
        }

        /// <summary>
        /// Lấy tổng tồn kho của một biến thể (Variant) trên tất cả các kho
        /// </summary>
        public int GetTotalStockByVariant(int variantId)
        {
            return db.WarehouseStocks
                     .Where(x => x.VariantId == variantId)
                     .Sum(x => x.Quantity) ?? 0;
        }

        // =============================================
        // 3. NHẬP KHO (STOCK IN)
        // =============================================

        public List<StockIn> ListStockIn()
        {
            return db.StockIns.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public int CreateStockIn(StockIn entity)
        {
            try
            {
                entity.CreatedAt = DateTime.Now;
                entity.Status = "pending"; // Chờ duyệt
                db.StockIns.Add(entity);
                db.SaveChanges();
                return entity.StockInId;
            }
            catch
            {
                return 0;
            }
        }

        public bool AddStockInDetail(StockInDetail detail)
        {
            try
            {
                detail.TotalPrice = detail.Quantity * detail.UnitPrice;
                db.StockInDetails.Add(detail);

                // Cập nhật tổng tiền phiếu nhập
                var stockIn = db.StockIns.Find(detail.StockInId);
                if (stockIn != null)
                {
                    stockIn.TotalAmount = (stockIn.TotalAmount) + detail.TotalPrice;
                }
                else
                {

                    stockIn.TotalAmount = 0 + detail.TotalPrice;
                }
                    db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Duyệt phiếu nhập kho (Gọi Stored Procedure)
        /// </summary>
        public bool ApproveStockIn(int stockInId, int userId)
        {
            try
            {
                object[] parameters =
                {
                    new SqlParameter("@StockInId", stockInId),
                    new SqlParameter("@ApprovedBy", userId)
                };

                // Gọi SP đã tạo trong CSDL
                db.Database.ExecuteSqlCommand("EXEC sp_ApproveStockIn @StockInId, @ApprovedBy", parameters);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =============================================
        // 4. XUẤT KHO (STOCK OUT)
        // =============================================

        public List<StockOut> ListStockOut()
        {
            return db.StockOuts.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public int CreateStockOut(StockOut entity)
        {
            try
            {
                entity.CreatedAt = DateTime.Now;
                entity.Status = "pending";
                db.StockOuts.Add(entity);
                db.SaveChanges();
                return entity.StockOutId;
            }
            catch
            {
                return 0;
            }
        }

        public bool AddStockOutDetail(StockOutDetail detail)
        {
            try
            {
                db.StockOutDetails.Add(detail);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Duyệt phiếu xuất kho (Gọi Stored Procedure)
        /// </summary>
        public bool ApproveStockOut(int stockOutId, int userId)
        {
            try
            {
                object[] parameters =
                {
                    new SqlParameter("@StockOutId", stockOutId),
                    new SqlParameter("@ApprovedBy", userId)
                };

                db.Database.ExecuteSqlCommand("EXEC sp_ApproveStockOut @StockOutId, @ApprovedBy", parameters);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Tạo phiếu xuất kho tự động từ đơn hàng (Gọi SP)
        /// </summary>
        public bool CreateStockOutFromOrder(int orderId, int warehouseId, int userId)
        {
            try
            {
                object[] parameters =
                {
                    new SqlParameter("@OrderId", orderId),
                    new SqlParameter("@WarehouseId", warehouseId),
                    new SqlParameter("@CreatedBy", userId)
                };

                db.Database.ExecuteSqlCommand("EXEC sp_CreateStockOutFromOrder @OrderId, @WarehouseId, @CreatedBy", parameters);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}