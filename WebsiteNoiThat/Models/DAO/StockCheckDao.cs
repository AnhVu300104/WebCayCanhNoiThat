// File: Models/DAO/StockCheckDao.cs

using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace Models.DAO
{
    public class StockCheckDao
    {
        DBNoiThat db = null;

        public StockCheckDao()
        {
            db = new DBNoiThat();
        }

        /// <summary>
        /// Thêm phiếu kiểm kê vào CSDL và trả về CheckId
        /// </summary>
        public int CreateStockCheck(StockCheck entity)
        {
            try
            {
                db.StockChecks.Add(entity);
                db.SaveChanges();
                return entity.CheckId;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                throw new Exception("Lỗi khi tạo phiếu kiểm kê: " + ex.Message);
            }
        }

        /// <summary>
        /// Thêm chi tiết kiểm kê
        /// </summary>
        public bool AddStockCheckDetail(StockCheckDetail detail)
        {
            try
            {
                db.StockCheckDetails.Add(detail);
                db.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách phiếu kiểm kê
        /// </summary>
        public List<StockCheck> GetAllStockChecks()
        {
            return db.StockChecks
                     .Include(sc => sc.Warehouse)
                     .Include(sc => sc.UserCreated)
                     .OrderByDescending(sc => sc.CheckDate)
                     .ToList();
        }

        /// <summary>
        /// Lấy chi tiết phiếu kiểm kê theo ID
        /// </summary>
        public StockCheck GetStockCheckById(int checkId)
        {
            return db.StockChecks
                     .Include(sc => sc.Warehouse)
                     .Include(sc => sc.UserCreated)
                     .Include(sc => sc.StockCheckDetails)
                     .FirstOrDefault(sc => sc.CheckId == checkId);
        }

        /// <summary>
        /// Duyệt phiếu kiểm kê và cập nhật tồn kho vào ProductVariant.StockQuantity
        /// </summary>
        public bool ApproveStockCheck(int checkId, int userId)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var check = db.StockChecks
                                  .Include(sc => sc.StockCheckDetails)
                                  .FirstOrDefault(sc => sc.CheckId == checkId);

                    if (check == null || check.Status != "Pending")
                    {
                        return false;
                    }

                    // 1. Cập nhật trạng thái phiếu kiểm kê
                    check.Status = "Approved";
                    check.ApprovedBy = userId;
                    check.CompletedAt = DateTime.Now;

                    // 2. Cập nhật tồn kho trực tiếp vào ProductVariant.StockQuantity
                    foreach (var detail in check.StockCheckDetails)
                    {
                        var variant = db.ProductVariants.Find(detail.VariantId);

                        if (variant != null)
                        {
                            int quantityBefore = variant.StockQuantity ?? 0;
                            int quantityChange = detail.ActualQuantity - quantityBefore;

                            // Cập nhật số lượng tồn kho
                            variant.StockQuantity = detail.ActualQuantity;

                            // 3. Ghi lịch sử giao dịch nếu có chênh lệch (nếu có bảng StockHistory)
                            if (quantityChange != 0)
                            {
                                try
                                {
                                    var stockHistory = new StockHistory
                                    {
                                        WarehouseId = check.WarehouseId,
                                        VariantId = detail.VariantId,
                                        Type = quantityChange > 0 ? "IN" : "OUT",
                                        ReferenceType = "StockCheck",
                                        ReferenceId = check.CheckId,
                                        QuantityBefore = quantityBefore,
                                        QuantityChange = Math.Abs(quantityChange),
                                        QuantityAfter = detail.ActualQuantity,
                                        Note = $"Kiểm kê kho - Chênh lệch: {(quantityChange > 0 ? "+" : "")}{quantityChange}",
                                        CreatedBy = userId,
                                        CreatedAt = DateTime.Now
                                    };

                                    db.StockHistories.Add(stockHistory);
                                }
                                catch
                                {
                                    // Bỏ qua nếu không có bảng StockHistory
                                }
                            }
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Ghi log lỗi
                    throw new Exception("Lỗi khi duyệt phiếu kiểm kê: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Lấy lịch sử giao dịch của sản phẩm trong kho
        /// </summary>
        public List<StockHistory> GetTransactionHistory(int warehouseId, int variantId)
        {
            try
            {
                return db.StockHistories
                         .Include(h => h.UserCreated)
                         .Where(h => h.WarehouseId == warehouseId && h.VariantId == variantId)
                         .OrderByDescending(h => h.CreatedAt)
                         .ToList();
            }
            catch
            {
                return new List<StockHistory>();
            }
        }

        /// <summary>
        /// Lấy danh sách biến thể theo kho
        /// </summary>
        public List<object> GetVariantsByWarehouse(int warehouseId)
        {
            return db.ProductVariants
                     .Include(v => v.Product)
                     .Where(v => v.WarehouseId == warehouseId && v.IsActive == true)
                     .Select(v => new
                     {
                         v.VariantId,
                         v.SKU,
                         ProductName = v.Product.Name,
                         StockQuantity = v.StockQuantity ?? 0
                     })
                     .ToList<object>();
        }
    }
}