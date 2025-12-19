using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class VariantController : Controller
    {
        private DBNoiThat db = new DBNoiThat();
        private AttributeDao attrDao = new AttributeDao();

        // =============================================
        // 1. DANH SÁCH BIẾN THỂ CỦA SẢN PHẨM
        // =============================================
        [HasCredential(RoleId = "VIEW_PRODUCT")]
        public ActionResult Index(int productId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            // Lấy sản phẩm
            var product = db.Products.Find(productId);
            if (product == null)
                return RedirectToAction("Show", "Product");

            ViewBag.Product = product;

            // Lấy danh sách biến thể (kèm thuộc tính + kho)
            var variants = db.ProductVariants
                .Include("VariantAttributeValues")
                .Include("VariantAttributeValues.Attribute")
                .Include("VariantAttributeValues.AttributeValue")
                .Include("Warehouse")
                .Where(v => v.ProductId == productId)
                .OrderByDescending(v => v.VariantId)
                .ToList();

            // ✅ TÍNH TỔNG TỒN KHO TỪ BIẾN THỂ
            // ✅ TÍNH TỔNG TỒN KHO TỪ BIẾN THỂ
            try
            {
                var totalStock = db.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .Sum(v => (int?)v.StockQuantity) ?? 0;

                ViewBag.TotalStock = totalStock;
                ViewBag.StockError = null; // Không có lỗi
            }
            catch (Exception ex)
            {
                ViewBag.TotalStock = 0;
                ViewBag.StockError = ex.Message; // Lưu lỗi vào ViewBag
                ViewBag.StackTrace = ex.StackTrace; // Chi tiết lỗi
            }
            return View(variants);
        }


        // =============================================
        // 2. TẠO BIẾN THỂ MỚI
        // =============================================
        [HttpGet]
        [HasCredential(RoleId = "ADD_PRODUCT")]
        public ActionResult Create(int productId)
        {
            var product = db.Products.Find(productId);
            if (product == null) return RedirectToAction("Index", "Product");

            ViewBag.Product = product;

            // Lấy thuộc tính và giá trị
            ViewBag.Attributes = attrDao.GetAttributesWithValues();

            // Lấy danh sách kho ACTIVE để chọn
            ViewBag.Warehouses = db.Warehouses
                                   .Where(w => w.IsActive == true)
                                   .OrderBy(w => w.Name)
                                   .ToList();

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        [HasCredential(RoleId = "ADD_PRODUCT")]
        public ActionResult Create(ProductVariant model, HttpPostedFileBase ImageFile, int[] AttributeIds, int[] ValueIds)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Xử lý ảnh
                    if (ImageFile != null && ImageFile.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                        string extension = Path.GetExtension(ImageFile.FileName);
                        fileName = fileName + "_" + long.Parse(DateTime.Now.ToString("yyyyMMddhhmmss")) + extension;
                        model.ImageVariant = fileName;
                        string path = Path.Combine(Server.MapPath("~/image/"), fileName);
                        ImageFile.SaveAs(path);
                    }

                    // 2. Tự sinh SKU nếu bỏ trống
                    if (string.IsNullOrEmpty(model.SKU))
                    {
                        model.SKU = "SP" + model.ProductId + "-" + DateTime.Now.ToString("HHmmss");
                    }

                    // 3. Thiết lập thông tin mặc định
                    model.IsActive = true;
                    model.CreatedAt = DateTime.Now;

                    // Lưu biến thể
                    db.ProductVariants.Add(model);
                    db.SaveChanges();

                    // 4. Lưu thuộc tính (Màu, Size)
                    if (AttributeIds != null && ValueIds != null && AttributeIds.Length == ValueIds.Length)
                    {
                        for (int i = 0; i < AttributeIds.Length; i++)
                        {
                            if (ValueIds[i] > 0)
                            {
                                var variantAttr = new VariantAttributeValue
                                {
                                    VariantId = model.VariantId,
                                    AttributeId = AttributeIds[i],
                                    ValueId = ValueIds[i]
                                };
                                db.VariantAttributeValues.Add(variantAttr);
                            }
                        }
                        db.SaveChanges();
                    }

                    // 5. Cập nhật tổng tồn kho
                    UpdateProductTotalQuantity(model.ProductId);

                    TempData["Success"] = "Thêm biến thể thành công";
                    return RedirectToAction("Index", new { productId = model.ProductId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            // Load lại dữ liệu nếu lỗi
            var prod = db.Products.Find(model.ProductId);
            ViewBag.Product = prod;
            ViewBag.Attributes = attrDao.GetAttributesWithValues();
            ViewBag.Warehouses = db.Warehouses.Where(w => w.IsActive == true).OrderBy(w => w.Name).ToList();
            return View(model);
        }

        // =============================================
        // 3. CHỈNH SỬA BIẾN THỂ
        // =============================================
        [HttpGet]
        [HasCredential(RoleId = "EDIT_PRODUCT")]
        public ActionResult Edit(int id)
        {
            var variant = db.ProductVariants.Find(id);
            if (variant == null) return HttpNotFound();

            ViewBag.Product = db.Products.Find(variant.ProductId);
            ViewBag.Attributes = attrDao.GetAttributesWithValues();
            ViewBag.SelectedValues = db.VariantAttributeValues
                                       .Where(x => x.VariantId == id)
                                       .ToList();

            // Lấy danh sách kho ACTIVE
            ViewBag.Warehouses = db.Warehouses
                                   .Where(w => w.IsActive == true)
                                   .OrderBy(w => w.Name)
                                   .ToList();

            return View(variant);
        }

        [HttpPost]
        [ValidateInput(false)]
        [HasCredential(RoleId = "EDIT_PRODUCT")]
        public ActionResult Edit(ProductVariant model, HttpPostedFileBase ImageFile, int[] AttributeIds, int[] ValueIds)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingVariant = db.ProductVariants.Find(model.VariantId);

                    // 1. Cập nhật thông tin cơ bản
                    existingVariant.Price = model.Price;
                    existingVariant.SalePrice = model.SalePrice;
                    existingVariant.StockQuantity = model.StockQuantity;
                    existingVariant.SKU = model.SKU;
                    existingVariant.IsActive = model.IsActive;
                    existingVariant.WarehouseId = model.WarehouseId; // THÊM MỚI

                    // 2. Xử lý ảnh
                    if (ImageFile != null && ImageFile.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                        string extension = Path.GetExtension(ImageFile.FileName);
                        fileName = fileName + "_" + long.Parse(DateTime.Now.ToString("yyyyMMddhhmmss")) + extension;
                        existingVariant.ImageVariant = fileName;
                        string path = Path.Combine(Server.MapPath("~/image/"), fileName);
                        ImageFile.SaveAs(path);
                    }

                    // 3. Cập nhật thuộc tính (Xóa cũ -> Thêm mới)
                    var oldAttrs = db.VariantAttributeValues.Where(x => x.VariantId == model.VariantId);
                    db.VariantAttributeValues.RemoveRange(oldAttrs);

                    if (AttributeIds != null && ValueIds != null)
                    {
                        for (int i = 0; i < AttributeIds.Length; i++)
                        {
                            if (ValueIds[i] > 0)
                            {
                                var variantAttr = new VariantAttributeValue
                                {
                                    VariantId = model.VariantId,
                                    AttributeId = AttributeIds[i],
                                    ValueId = ValueIds[i]
                                };
                                db.VariantAttributeValues.Add(variantAttr);
                            }
                        }
                    }

                    db.SaveChanges();

                    // 4. Cập nhật tồn kho tổng
                    UpdateProductTotalQuantity(existingVariant.ProductId);

                    TempData["Success"] = "Cập nhật biến thể thành công";
                    return RedirectToAction("Index", new { productId = existingVariant.ProductId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            ViewBag.Product = db.Products.Find(model.ProductId);
            ViewBag.Attributes = attrDao.GetAttributesWithValues();
            ViewBag.SelectedValues = db.VariantAttributeValues.Where(x => x.VariantId == model.VariantId).ToList();
            ViewBag.Warehouses = db.Warehouses.Where(w => w.IsActive == true).OrderBy(w => w.Name).ToList();
            return View(model);
        }

        // =============================================
        // 4. XÓA BIẾN THỂ
        // =============================================
        [HasCredential(RoleId = "DELETE_PRODUCT")]
        public ActionResult Delete(int id)
        {
            var variant = db.ProductVariants.Find(id);
            int productId = 0;
            if (variant != null)
            {
                productId = variant.ProductId;

                // Xóa thuộc tính
                var attrs = db.VariantAttributeValues.Where(x => x.VariantId == id);
                db.VariantAttributeValues.RemoveRange(attrs);

                db.ProductVariants.Remove(variant);
                db.SaveChanges();

                UpdateProductTotalQuantity(productId);
                TempData["Success"] = "Xóa biến thể thành công";
            }
            return RedirectToAction("Index", new { productId = productId });
        }

        // Helper: Cập nhật tổng số lượng tồn kho
        private void UpdateProductTotalQuantity(int productId)
        {
            var totalQty = db.ProductVariants
                             .Where(x => x.ProductId == productId && x.IsActive == true)
                             .Sum(x => (int?)x.StockQuantity) ?? 0;

            var product = db.Products.Find(productId);
            if (product != null)
            {
                product.Quantity = totalQty;
                db.SaveChanges();
            }
        }
    }
}