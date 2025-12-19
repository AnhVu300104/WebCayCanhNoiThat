using Models.DAO;
using Models.EF;
using Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebsiteNoiThat.Models;
using PagedList;
using System.Data.Entity;
namespace WebsiteNoiThat.Controllers
{
    public class ProductController : Controller
    {
        private ProductDao productDao = new ProductDao();
        private AttributeDao attrDao = new AttributeDao();
        private DBNoiThat db = new DBNoiThat();

        // =============================================
        // CHI TIẾT SẢN PHẨM (Có Biến Thể)
        // =============================================
        public ActionResult Details(int id)
        {
            // Lấy thông tin sản phẩm chính (chỉ active)
            var product = productDao.DetailsProduct(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Tạo ViewModel
            var model = new ProductDetailViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                Photo = product.Photo,
                StartDate = product.StartDate,
                EndDate = product.EndDate,
                Discount = product.Discount,
                IsActive = product.IsActive
            };

            // ✅ QUAN TRỌNG: Include đầy đủ các quan hệ
            var variants = db.ProductVariants
                             .Include("VariantAttributeValues")
                             .Include("VariantAttributeValues.Attribute")
                             .Include("VariantAttributeValues.AttributeValue")
                             .Include("Warehouse")
                             .Where(v => v.ProductId == id && v.IsActive == true)
                             .OrderByDescending(v => v.VariantId)
                             .ToList();

            // Debug: kiểm tra dữ liệu
            var debugMessages = new List<string>();
            foreach (var v in variants)
            {
                debugMessages.Add($"VariantId: {v.VariantId}, SKU: {v.SKU}, Price: {v.Price}, SalePrice: {v.SalePrice}, Stock: {v.StockQuantity}");

                if (v.VariantAttributeValues != null && v.VariantAttributeValues.Any())
                {
                    foreach (var vav in v.VariantAttributeValues)
                    {
                        var attrName = vav.Attribute?.Name ?? "NULL";
                        var attrValue = vav.AttributeValue?.Value ?? "NULL";
                        var displayValue = vav.AttributeValue?.DisplayValue ?? "NULL";
                        debugMessages.Add($"   → AttributeId: {vav.AttributeId}, ValueId: {vav.ValueId}");
                        debugMessages.Add($"   → Attribute: {attrName}, Value: {attrValue}, Display: {displayValue}");
                    }
                }
                else
                {
                    debugMessages.Add("   ✗ No VariantAttributeValues");
                }
            }
            ViewBag.DebugMessages = debugMessages;

            // Chuyển sang ViewModel
            model.Variants = variants.Select(v => new ProductVariantViewModel
            {
                VariantId = v.VariantId,
                SKU = v.SKU,
                Price = v.Price ,
                SalePrice = v.SalePrice ?? 0,
                StockQuantity = v.StockQuantity,
                Image = v.ImageVariant ?? product.Photo,

                // Lấy AttributesInfo
                AttributesInfo = v.VariantAttributeValues
                                 .Where(vav => vav.Attribute != null && vav.AttributeValue != null)
                                 .OrderBy(vav => vav.Attribute.DisplayOrder)
                                 .Select(vav => vav.Attribute.Name + ": " + (vav.AttributeValue.DisplayValue ?? vav.AttributeValue.Value))
                                 .ToList(),

                // Lấy VariantName
                VariantName = v.VariantAttributeValues != null && v.VariantAttributeValues.Any()
                              ? string.Join(", ", v.VariantAttributeValues
                                .Where(vav => vav.AttributeValue != null)
                                .OrderBy(vav => vav.Attribute?.DisplayOrder ?? 999)
                                .Select(vav => vav.AttributeValue.DisplayValue ?? vav.AttributeValue.Value))
                              : "Mặc định",

                // ✅ THÊM: Lấy VariantAttributeValues để hiển thị badge
                VariantAttributeValues = v.VariantAttributeValues.ToList()

            }).ToList();

            // Nếu không có biến thể, tạo mặc định
            if (!model.Variants.Any())
            {
                model.Variants = new List<ProductVariantViewModel>
        {
            new ProductVariantViewModel
            {
                VariantId = 0,
                SKU = "MACDINH-" + product.ProductId,
                Price = product.Price ?? 0,
                SalePrice = product.Price ?? 0,
                StockQuantity = product.Quantity,
                Image = product.Photo,
                VariantName = "Mặc định"
            }
        };
            }

            // Sản phẩm cùng danh mục
            ViewBag.RelatedProducts = db.Products
                .Where(p => p.CateId == product.CateId && p.ProductId != id && p.IsActive)
                .Take(8)
                .ToList();

            return View(model);
        }

        // =============================================
        // LẤY GIÁ CỦA BIẾN THỂ (AJAX)
        // =============================================
        [HttpPost]
        public JsonResult GetVariantPrice(int variantId)
        {
            var variant = db.ProductVariants.Find(variantId);
            if (variant == null || variant.IsActive != true)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                price = variant.Price,
                salePrice = variant.SalePrice,
                stock = variant.StockQuantity,
                image = variant.ImageVariant
            });
        }

        // =============================================
        // LẤY DANH SÁCH BIẾN THỂ (AJAX)
        // =============================================
        [HttpPost]
        public JsonResult GetVariants(int productId)
        {
            var variants = db.ProductVariants
                             .Include("VariantAttributeValues")
                             .Include("VariantAttributeValues.Attribute")
                             .Include("VariantAttributeValues.AttributeValue")
                             .Where(v => v.ProductId == productId && v.IsActive == true)
                             .Select(v => new
                             {
                                 variantId = v.VariantId,
                                 sku = v.SKU,
                                 price = v.Price,
                                 salePrice = v.SalePrice,
                                 stock = v.StockQuantity,
                                 image = v.ImageVariant,
                                 attributes = v.VariantAttributeValues
                                             .OrderBy(vav => vav.Attribute.DisplayOrder)
                                             .Select(vav => new
                                             {
                                                 attributeId = vav.AttributeId,
                                                 attributeName = vav.Attribute.Name,
                                                 valueId = vav.ValueId,
                                                 valueName = vav.AttributeValue.DisplayValue
                                             }).ToList()
                             })
                             .ToList();

            return Json(new { success = true, data = variants });
        }

        // =============================================
        // TÌM BIẾN THỂ THEO THUỘC TÍNH (AJAX)
        // =============================================
        [HttpPost]
        public JsonResult FindVariantByAttributes(int productId, string attributeValues)
        {
            // attributeValues format: "1-5,2-8" (AttributeId-ValueId)
            var pairs = attributeValues.Split(',')
                .Select(x => x.Split('-'))
                .Select(x => new { AttrId = int.Parse(x[0]), ValId = int.Parse(x[1]) })
                .ToList();

            var variant = db.ProductVariants
                            .Include("VariantAttributeValues")
                            .Where(v => v.ProductId == productId && v.IsActive == true)
                            .AsEnumerable()
                            .FirstOrDefault(v =>
                                pairs.All(p =>
                                    v.VariantAttributeValues.Any(vav =>
                                        vav.AttributeId == p.AttrId && vav.ValueId == p.ValId
                                    )
                                ) && v.VariantAttributeValues.Count == pairs.Count
                            );

            if (variant == null)
            {
                return Json(new { success = false, message = "Không tìm thấy biến thể phù hợp" });
            }

            return Json(new
            {
                success = true,
                variantId = variant.VariantId,
                price = variant.Price,
                salePrice = variant.SalePrice,
                stock = variant.StockQuantity,
                image = variant.ImageVariant
            });
        }

        // =============================================
        // DANH SÁCH SẢN PHẨM (Dự phòng)
        // =============================================
        public ActionResult Index(int page = 1, int pagesize = 12)
        {
            var products = db.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToList();

            int total = db.Products.Count(p => p.IsActive);
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.TotalPages = (total / pagesize) + (total % pagesize > 0 ? 1 : 0);

            return View(products);
        }
        //
        public ActionResult ProductHot()
        {
            var model = new ProductDao().ProductHot();
            return PartialView(model);
        }
        public ActionResult SaleProduct()
        {
            var model = new ProductDao().SaleProduct();
            return PartialView(model);
        }
        public ActionResult NewProduct()
        {
            var model = new ProductDao().NewProduct();
            return PartialView(model);
        }
        public ActionResult ShowProduct(int page = 1, int pagesize = 16)
        {
            var query = db.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name);

            var model = query.ToPagedList(page, pagesize);

            return View(model);
        }
        public ActionResult Search(string keyword, int page = 1, int pagesize = 16)
        {
            if (string.IsNullOrEmpty(keyword))
                return RedirectToAction("Index");

            int total = 0;
            var model = productDao.Search(keyword, ref total, page, pagesize);

            ViewBag.Keyword = keyword;
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)System.Math.Ceiling((double)total / pagesize);

            return View(model);
        }


        public ActionResult SearchFocus(
      bool? check0, bool? check1, bool? check2, bool? check3, bool? check4,
      int page = 1)
        {
            int pagesize = 16;

            var query = db.Products.Where(p => p.IsActive);

            // Lọc theo giá thực tế (giá sau giảm)
            query = query.Where(p =>
                (check0 == true && ((p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) < 100000)) ||
                (check1 == true && ((p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) >= 100000
                                  && (p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) < 200000)) ||
                (check2 == true && ((p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) >= 200000
                                  && (p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) < 300000)) ||
                (check3 == true && ((p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) >= 300000
                                  && (p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) < 400000)) ||
                (check4 == true && ((p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price) >= 400000))
            );

            // Sắp xếp theo giá thực tế tăng dần
            query = query.OrderBy(p => (p.Discount > 0 ? p.Price * (100 - p.Discount) / 100 : p.Price));

            var model = query.ToPagedList(page, pagesize);

            // Giữ trạng thái checkbox
            ViewBag.check0 = check0;
            ViewBag.check1 = check1;
            ViewBag.check2 = check2;
            ViewBag.check3 = check3;
            ViewBag.check4 = check4;

            return View(model);
        }



        public JsonResult ListName(string q)
        {
            var data = productDao.ListName(q);
            return Json(new
            {
                data = data,
                status = true
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Sales(int page = 1, int pagesize = 16)
        {
            var query = db.Products
                .Where(p => p.IsActive && p.Discount > 0)
                .OrderByDescending(p => p.Discount);

            var model = query.ToPagedList(page, pagesize);

            return View(model);
        }
        public ActionResult CategoryShow(int cateId, int page = 1, int pagesize = 16)
        {
            var category = new CategoryDao().ViewDetail(cateId);
            if (category == null || !category.IsActive)
                return HttpNotFound();

            ViewBag.CategoryShow = category;

            int total = 0;
            var model = productDao.ListByCategoryId(cateId, ref total, page, pagesize);

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pagesize);

            return View(model); // model là List<Product>
        }



        public ActionResult Hots(int page = 1, int pagesize = 16)
        {
            var query = productDao.ProductHot();

            int total = query.Count();

            var model = query.ToPagedList(page, pagesize);

            return View(model);
        }


    }
}