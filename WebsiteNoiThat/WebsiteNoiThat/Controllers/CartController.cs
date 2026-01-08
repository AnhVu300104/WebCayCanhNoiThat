using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;
using WebsiteNoiThat.Models.ViewModels;
using CartItem=Models.EF.CartItem;
namespace WebsiteNoiThat.Controllers
{
    public class CartController : Controller
    {
        private DBNoiThat db = new DBNoiThat();
        private OrderDao orderDao = new OrderDao();
        private OrderDetailDao orderDetailDao = new OrderDetailDao();

        // ==============================
        // XEM GIỎ HÀNG
        // ==============================
        public ActionResult Index()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null) return Redirect("/dang-nhap");

            var cartItems = db.GioHang
                .Where(c => c.UserId == session.UserId)
                .ToList();

            var list = new List<GHViewModel>();
            foreach (var gh in cartItems)
            {
                var product = db.Products.Find(gh.ProductId);
                if (product == null) continue;

                string variantInfo = gh.VariantInfo;
                ProductVariant variant = null;

                if (gh.VariantId.HasValue)
                {
                    variant = db.ProductVariants.Find(gh.VariantId);

                    if (string.IsNullOrEmpty(variantInfo) && variant != null)
                    {
                        var vavs = db.VariantAttributeValues
                            .Include("Attribute")
                            .Include("AttributeValue")
                            .Where(vav => vav.VariantId == gh.VariantId)
                            .OrderBy(vav => vav.Attribute.DisplayOrder)
                            .ToList();

                        if (vavs.Any())
                        {
                            variantInfo = string.Join(", ", vavs
                                .Where(vav => vav.Attribute != null && vav.AttributeValue != null)
                                .Select(vav => vav.Attribute.Name + ": " + (vav.AttributeValue.DisplayValue ?? vav.AttributeValue.Value)));
                        }
                    }
                }

                if (string.IsNullOrEmpty(variantInfo))
                {
                    variantInfo = "Mặc định";
                }

                list.Add(new GHViewModel
                {
                    CartItemId = gh.GioHangId,
                    ProductId = gh.ProductId,
                    ProductName = product.Name,
                    VariantId = gh.VariantId,
                    VariantInfo = variantInfo,
                    Quantity = gh.Quantity,
                    Price = (int)(variant?.SalePrice ?? variant?.Price ?? product.Price ?? 0),
                    MaxQuantity = variant?.StockQuantity ?? product.Quantity ?? 0,
                    Photo = variant?.ImageVariant ?? product.Photo,
                    CreateDate = gh.CreateDate,
                    UpdateDate = gh.UpdateDate
                });
            }

            return View(list);
        }

        // ==============================
        // THÊM SẢN PHẨM VÀO GIỎ
        // ==============================
        [HttpGet]
        public ActionResult AddCartByGet(int productId, int? variantId, int quantity = 1)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null) return Redirect("/dang-nhap");

            var product = db.Products.Find(productId);
            if (product == null || !product.IsActive)
                return RedirectToAction("Index");

            var variant = variantId.HasValue ? db.ProductVariants.Find(variantId) : null;
            int stock = variant?.StockQuantity ?? product.Quantity ?? 0;

            var existing = db.GioHang
                .FirstOrDefault(c => c.UserId == session.UserId
                    && c.ProductId == productId
                    && c.VariantId == variantId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                if (existing.Quantity > stock)
                    existing.Quantity = stock;
                existing.UpdateDate = DateTime.Now;
                db.Entry(existing).State = System.Data.Entity.EntityState.Modified;
            }
            else
            {
                string variantInfo = null;
                if (variantId.HasValue)
                {
                    var vavs = db.VariantAttributeValues
                        .Include("Attribute")
                        .Include("AttributeValue")
                        .Where(vav => vav.VariantId == variantId)
                        .OrderBy(vav => vav.Attribute.DisplayOrder)
                        .ToList();

                    if (vavs.Any())
                    {
                        variantInfo = string.Join(", ", vavs
                            .Where(vav => vav.Attribute != null && vav.AttributeValue != null)
                            .Select(vav => vav.Attribute.Name + ": " + (vav.AttributeValue.DisplayValue ?? vav.AttributeValue.Value)));
                    }
                }

                var gh = new GioHang
                {
                    UserId = session.UserId,
                    ProductId = productId,
                    ProductName = product.Name,
                    VariantId = variantId,
                    VariantInfo = variantInfo,
                    Quantity = quantity > stock ? stock : quantity,
                    CreateDate = DateTime.Now
                };
                db.GioHang.Add(gh);
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult AddCart(int productId, int? variantId, int quantity = 1)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null) return Redirect("/dang-nhap");

            var product = db.Products.Find(productId);
            if (product == null || !product.IsActive)
                return RedirectToAction("Index");

            var variant = variantId.HasValue ? db.ProductVariants.Find(variantId) : null;
            int stock = variant?.StockQuantity ?? product.Quantity ?? 0;

            var existing = db.GioHang
                .FirstOrDefault(c => c.UserId == session.UserId
                    && c.ProductId == productId
                    && c.VariantId == variantId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                if (existing.Quantity > stock)
                    existing.Quantity = stock;
                existing.UpdateDate = DateTime.Now;
                db.Entry(existing).State = System.Data.Entity.EntityState.Modified;
            }
            else
            {
                string variantInfo = null;
                if (variantId.HasValue)
                {
                    var vavs = db.VariantAttributeValues
                        .Include("Attribute")
                        .Include("AttributeValue")
                        .Where(vav => vav.VariantId == variantId)
                        .OrderBy(vav => vav.Attribute.DisplayOrder)
                        .ToList();

                    if (vavs.Any())
                    {
                        variantInfo = string.Join(", ", vavs.Select(v => v.Attribute.Name + ": " + v.AttributeValue.DisplayValue));
                    }
                }

                var gh = new GioHang
                {
                    UserId = session.UserId,
                    ProductId = productId,
                    ProductName = product.Name,
                    VariantId = variantId,
                    VariantInfo = variantInfo,
                    Quantity = quantity > stock ? stock : quantity,
                    CreateDate = DateTime.Now
                };
                db.GioHang.Add(gh);
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }
        // XÓA CÁC SẢN PHẨM ĐÃ CHỌN
        [HttpPost]
        
        public ActionResult DeleteSelected(int[] cartItemIds)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            if (cartItemIds == null || cartItemIds.Length == 0)
                return Json(new { success = false, message = "Không có sản phẩm nào được chọn" });

            try
            {
                var items = db.GioHang
                    .Where(c => c.UserId == session.UserId && cartItemIds.Contains(c.GioHangId))
                    .ToList();

                if (items.Any())
                {
                    db.GioHang.RemoveRange(items);
                    db.SaveChanges();
                    return Json(new { success = true, message = $"Đã xóa {items.Count} sản phẩm" });
                }

                return Json(new { success = false, message = "Không tìm thấy sản phẩm để xóa" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpPost]
        
        public ActionResult DeleteItem(int cartItemId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var item = db.GioHang.Find(cartItemId);
            if (item != null && item.UserId == session.UserId)
            {
                db.GioHang.Remove(item);
                db.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
        }

        [HttpPost]
        
        public ActionResult DeleteAll()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var items = db.GioHang.Where(c => c.UserId == session.UserId).ToList();
            db.GioHang.RemoveRange(items);
            db.SaveChanges();

            return Json(new { success = true });
        }
        // ==============================
        // CẬP NHẬT SỐ LƯỢNG
        // ==============================
        [HttpPost]
        public ActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null) return Redirect("/dang-nhap");

            var item = db.GioHang.Find(cartItemId);
            if (item != null && item.UserId == session.UserId)
            {
                var product = db.Products.Find(item.ProductId);
                var variant = item.VariantId.HasValue ? db.ProductVariants.Find(item.VariantId) : null;
                int maxQuantity = variant?.StockQuantity ?? product.Quantity ?? 0;

                item.Quantity = Math.Min(quantity, maxQuantity);
                if (item.Quantity < 1) item.Quantity = 1;
                item.UpdateDate = DateTime.Now;
                db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        //        [HttpPost]
        //        public ActionResult Checkout(
        //    CheckoutViewModel model,
        //    string paymentMethod,
        //    string Note
        //)
        //        {
        //            // 1. Kiểm tra đăng nhập
        //            var session = (UserLogin)Session[Commoncontent.user_sesion];
        //            if (session == null)
        //                return Redirect("/dang-nhap");

        //            // 2. Lấy danh sách ID sản phẩm đã chọn từ Session
        //            var selectedItems = Session["SelectedCartItems"] as int[];
        //            if (selectedItems == null || selectedItems.Length == 0)
        //            {
        //                TempData["Error"] =
        //                    "Phiên làm việc đã hết hạn hoặc chưa chọn sản phẩm. Vui lòng thử lại!";
        //                return RedirectToAction("Index");
        //            }

        //            // 3. Validate dữ liệu
        //            if (!ModelState.IsValid)
        //            {
        //                var cartItemsForView = db.GioHang
        //                    .Where(c =>
        //                        c.UserId == session.UserId &&
        //                        selectedItems.Contains(c.GioHangId)
        //                    )
        //                    .ToList();

        //                ViewBag.CartItems = GetCartItemsViewModel(cartItemsForView);
        //                return View("Checkout", model);
        //            }

        //            using (var transaction = db.Database.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // 4. Tạo đơn hàng
        //                    var order = new Order
        //                    {
        //                        UserId = session.UserId,
        //                        ShipName = model.ShipName,
        //                        ShipPhone = model.ShipPhone,
        //                        ShipEmail = model.ShipEmail,
        //                        ShipAddress = model.ShipAddress,
        //                        CreatedDate = DateTime.Now,
        //                        UpdateDate = DateTime.Now,
        //                        StatusId = 1 // 1: Chờ xử lý
        //                    };

        //                    int orderId = orderDao.Insert(order);

        //                    // 5. Lấy sản phẩm từ giỏ hàng
        //                    var cartItems = db.GioHang
        //                        .Where(c =>
        //                            c.UserId == session.UserId &&
        //                            selectedItems.Contains(c.GioHangId)
        //                        )
        //                        .ToList();

        //                    decimal totalAmount = 0;

        //                    foreach (var gh in cartItems)
        //                    {
        //                        var product = db.Products.Find(gh.ProductId);
        //                        var variant = gh.VariantId.HasValue
        //                            ? db.ProductVariants.Find(gh.VariantId)
        //                            : null;

        //                        int currentPrice = (int)(
        //                            variant?.SalePrice ??
        //                            variant?.Price ??
        //                            product?.Price ??
        //                            0
        //                        );

        //                        totalAmount += currentPrice * gh.Quantity;

        //                        var detail = new OrderDetail
        //                        {
        //                            OrderId = orderId,
        //                            ProductId = gh.ProductId,
        //                            VariantId = gh.VariantId,
        //                            VariantInfo = gh.VariantInfo,
        //                            Quantity = gh.Quantity,
        //                            Price = currentPrice
        //                        };

        //                        orderDetailDao.Insert(detail);

        //                        // 6. Trừ tồn kho
        //                        if (variant != null)
        //                        {
        //                            if (variant.StockQuantity < gh.Quantity)
        //                                throw new Exception(
        //                                    $"Sản phẩm {product.Name} ({gh.VariantInfo}) không đủ tồn kho."
        //                                );

        //                            variant.StockQuantity -= gh.Quantity;
        //                        }
        //                        else if (product != null)
        //                        {
        //                            if (product.Quantity < gh.Quantity)
        //                                throw new Exception(
        //                                    $"Sản phẩm {product.Name} không đủ tồn kho."
        //                                );

        //                            product.Quantity -= gh.Quantity;
        //                        }
        //                    }

        //                    // 7. Xóa giỏ hàng
        //                    db.GioHang.RemoveRange(cartItems);
        //                    db.SaveChanges();
        //                    transaction.Commit();

        //                    Session.Remove("SelectedCartItems");

        //                    // 8. Gửi email
        //                    try
        //                    {
        //                        SendOrderEmail(
        //                            orderId,
        //                            model,
        //                            cartItems,
        //                            totalAmount,
        //                            paymentMethod,
        //                            Note
        //                        );

        //                        TempData["EmailSuccess"] =
        //                            "Email xác nhận đơn hàng đã được gửi!";
        //                    }
        //                    catch (Exception emailEx)
        //                    {
        //                        System.Diagnostics.Debug.WriteLine(
        //                            $"❌ Lỗi gửi email: {emailEx.Message}"
        //                        );
        //                        TempData["EmailError"] =
        //                            "Đặt hàng thành công nhưng không thể gửi email.";
        //                    }

        //                    // 9. Trang thành công
        //                    return RedirectToAction("OrderSuccess", new { id = orderId });
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();

        //                    System.Diagnostics.Debug.WriteLine(
        //                        $"❌ Lỗi checkout: {ex.Message}"
        //                    );

        //                    ModelState.AddModelError(
        //                        "",
        //                        "Có lỗi xảy ra khi xử lý đơn hàng: " + ex.Message
        //                    );

        //                    var cartItemsForView = db.GioHang
        //                        .Where(c =>
        //                            c.UserId == session.UserId &&
        //                            selectedItems.Contains(c.GioHangId)
        //                        )
        //                        .ToList();

        //                    ViewBag.CartItems = GetCartItemsViewModel(cartItemsForView);
        //                    return View("Checkout", model);
        //                }
        //            }
        //        }
        [HttpPost]
        public ActionResult Checkout(CheckoutViewModel model, string paymentMethod, string Note)
        {
            // 1. Kiểm tra đăng nhập
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null) return Redirect("/dang-nhap");

            // 2. Lấy danh sách sản phẩm đã chọn
            var selectedItems = Session["SelectedCartItems"] as int[];
            if (selectedItems == null || selectedItems.Length == 0)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn hoặc chưa chọn sản phẩm. Vui lòng thử lại!";
                return RedirectToAction("Index");
            }

            // 3. Validate dữ liệu
            if (!ModelState.IsValid)
            {
                var cartItemsForView = db.GioHang
                    .Where(c => c.UserId == session.UserId && selectedItems.Contains(c.GioHangId))
                    .ToList();
                ViewBag.CartItems = GetCartItemsViewModel(cartItemsForView);
                return View("Checkout", model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // ========================================
                    // 4. TẠO ĐƠN HÀNG
                    // ========================================
                    var order = new Order
                    {
                        UserId = session.UserId,
                        ShipName = model.ShipName,
                        ShipPhone = model.ShipPhone,
                        ShipEmail = model.ShipEmail,
                        ShipAddress = model.ShipAddress,
                        CreatedDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        StatusId = 1
                    };

                    db.Orders.Add(order);

                    // ⚠️ TEST SAVE ORDER TRƯỚC
                    try
                    {
                        db.SaveChanges();
                        System.Diagnostics.Debug.WriteLine($"✅ Đã tạo Order #{order.OrderId}");
                    }
                    catch (Exception exOrder)
                    {
                        throw new Exception($"LỖI KHI TẠO ORDER: {GetFullErrorMessage(exOrder)}");
                    }

                    int orderId = order.OrderId;

                    // ========================================
                    // 5. LẤY SẢN PHẨM TỪ GIỎ HÀNG
                    // ========================================
                    var cartItems = db.GioHang
                        .Where(c => c.UserId == session.UserId && selectedItems.Contains(c.GioHangId))
                        .ToList();

                    decimal totalAmount = 0;

                    // ========================================
                    // 6. TẠO CHI TIẾT ĐƠN HÀNG + TRỪ TỒN KHO
                    // ========================================
                    foreach (var gh in cartItems)
                    {
                        var product = db.Products.Find(gh.ProductId);
                        var variant = gh.VariantId.HasValue ? db.ProductVariants.Find(gh.VariantId) : null;

                        int currentPrice = (int)(variant?.SalePrice ?? variant?.Price ?? product?.Price ?? 0);
                        totalAmount += currentPrice * gh.Quantity;

                        // ✅ KIỂM TRA TỒN KHO
                        if (variant != null)
                        {
                            if (variant.StockQuantity < gh.Quantity)
                                throw new Exception($"Sản phẩm {product?.Name ?? "N/A"} ({gh.VariantInfo}) không đủ tồn kho. Còn: {variant.StockQuantity}, Yêu cầu: {gh.Quantity}");

                            variant.StockQuantity -= gh.Quantity;
                        }
                        else if (product != null)
                        {
                            if (product.Quantity < gh.Quantity)
                                throw new Exception($"Sản phẩm {product.Name} không đủ tồn kho. Còn: {product.Quantity}, Yêu cầu: {gh.Quantity}");

                            product.Quantity -= gh.Quantity;
                        }

                        // ✅ TẠO ORDERDETAIL
                        var detail = new OrderDetail
                        {
                            OrderId = orderId,
                            ProductId = gh.ProductId,
                            VariantId = gh.VariantId,
                            VariantInfo = gh.VariantInfo,
                            Quantity = gh.Quantity,
                            Price = currentPrice
                        };

                        db.OrderDetails.Add(detail);
                        System.Diagnostics.Debug.WriteLine($"  + Thêm OrderDetail: Product={gh.ProductId}, Variant={gh.VariantId}, Qty={gh.Quantity}");
                    }

                    // ========================================
                    // 7. XÓA GIỎ HÀNG
                    // ========================================
                    db.GioHang.RemoveRange(cartItems);

                    // ========================================
                    // 8. LƯU TẤT CẢ (PHẦN QUAN TRỌNG NHẤT)
                    // ========================================
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("⏳ Đang SaveChanges...");
                        db.SaveChanges();
                        System.Diagnostics.Debug.WriteLine("✅ SaveChanges thành công!");
                    }
                    catch (Exception exSave)
                    {
                        // 🔥 ĐÂY LÀ NƠI LỖI XẢY RA - HIỂN THỊ CHI TIẾT
                        throw new Exception($"LỖI KHI SAVE DATABASE: {GetFullErrorMessage(exSave)}");
                    }

                    transaction.Commit();
                    Session.Remove("SelectedCartItems");

                    // ========================================
                    // 9. GỬI EMAIL
                    // ========================================
                    try
                    {
                        SendOrderEmail(orderId, model,paymentMethod, Note);
                        TempData["EmailSuccess"] = "Email xác nhận đơn hàng đã được gửi!";
                    }
                    catch (Exception emailEx)
                    {
                        TempData["EmailError"] = "Đặt hàng thành công nhưng không thể gửi email.";
                    }

                    return RedirectToAction("OrderSuccess", new { id = orderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    // 🔥🔥🔥 HIỂN THỊ LỖI RA MÀN HÌNH 🔥🔥🔥
                    string fullError = GetFullErrorMessage(ex);

                    System.Diagnostics.Debug.WriteLine("❌❌❌❌❌❌❌❌❌❌");
                    System.Diagnostics.Debug.WriteLine(fullError);
                    System.Diagnostics.Debug.WriteLine("❌❌❌❌❌❌❌❌❌❌");

                    // RETURN CONTENT TRỰC TIẾP - KHÔNG QUA VIEW
                    return Content($@"
<html>
<head>
    <meta charset='utf-8'/>
    <title>LỖI CHECKOUT</title>
    <style>
        body {{ font-family: Arial; padding: 20px; background: #f5f5f5; }}
        .error-box {{ background: white; border: 3px solid #dc3545; padding: 20px; border-radius: 8px; }}
        .error-title {{ color: #dc3545; margin-top: 0; }}
        .error-content {{ background: #f8f9fa; padding: 15px; font-family: 'Courier New', monospace; 
                         white-space: pre-wrap; word-wrap: break-word; overflow-x: auto; 
                         border: 1px solid #ddd; max-height: 500px; overflow-y: auto; }}
        .btn {{ display: inline-block; margin-top: 20px; padding: 10px 20px; background: #007bff; 
               color: white; text-decoration: none; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='error-box'>
        <h1 class='error-title'>🔴 LỖI CHECKOUT - CHI TIẾT ĐẦY ĐỦ</h1>
        <div class='error-content'>{System.Web.HttpUtility.HtmlEncode(fullError)}</div>
        <a href='javascript:history.back()' class='btn'>← Quay lại</a>
    </div>
</body>
</html>
", "text/html");
                }
            }
        }

        // 🔥 HÀM LẤY TOÀN BỘ LỖI (THÊM VÀO CONTROLLER)
        private string GetFullErrorMessage(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==============================================");
            sb.AppendLine("           LỖI CHI TIẾT ĐẦY ĐỦ");
            sb.AppendLine("==============================================");
            sb.AppendLine();
            sb.AppendLine($"📌 Loại lỗi: {ex.GetType().FullName}");
            sb.AppendLine($"📌 Message: {ex.Message}");
            sb.AppendLine();

            // Lấy tất cả InnerException
            var innerEx = ex.InnerException;
            int level = 1;
            while (innerEx != null)
            {
                sb.AppendLine($"--- InnerException cấp {level} ---");
                sb.AppendLine($"Loại: {innerEx.GetType().FullName}");
                sb.AppendLine($"Message: {innerEx.Message}");
                sb.AppendLine();
                innerEx = innerEx.InnerException;
                level++;
            }

            // Kiểm tra DbEntityValidationException
            if (ex is System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                sb.AppendLine("=== VALIDATION ERRORS ===");
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    sb.AppendLine($"Entity: {validationErrors.Entry.Entity.GetType().Name}");
                    sb.AppendLine($"State: {validationErrors.Entry.State}");
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        sb.AppendLine($"  ❌ Property: {validationError.PropertyName}");
                        sb.AppendLine($"     Error: {validationError.ErrorMessage}");
                    }
                    sb.AppendLine();
                }
            }

            // Stack Trace
            sb.AppendLine("=== STACK TRACE ===");
            sb.AppendLine(ex.StackTrace);

            return sb.ToString();
        }
        // ==============================
        // THANH TOÁN
        // ==============================
        //[HttpGet]
        //public ActionResult Checkout()
        //{
        //    var session = (UserLogin)Session[Commoncontent.user_sesion];
        //    if (session == null) return Redirect("/dang-nhap");

        //    var cartItems = GetCartItems(session.UserId);
        //    if (!cartItems.Any()) return RedirectToAction("Index");

        //    var user = db.Users.Find(session.UserId);
        //    var model = new CheckoutViewModel
        //    {
        //        ShipName = user.Name,
        //        ShipPhone = user.Phone.ToString(),
        //        ShipEmail = user.Email,
        //        ShipAddress = user.Address
        //    };

        //    ViewBag.CartItems = cartItems;
        //    return View(model);
        //}

        [HttpPost]
        
        public ActionResult Checkoutselected(int[] selectedItems)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion];
            if (session == null) return Redirect("/dang-nhap");

            // Kiểm tra có sản phẩm được chọn không
            if (selectedItems == null || selectedItems.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán!";
                return RedirectToAction("Index");
            }

            // Lấy chỉ những sản phẩm đã được chọn
            var cartItems = db.GioHang
                .Where(c => c.UserId == session.UserId && selectedItems.Contains(c.GioHangId))
                .ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Không tìm thấy sản phẩm đã chọn!";
                return RedirectToAction("Index");
            }

            // Chuyển đổi sang ViewModel
            var selectedCartItems = new List<GHViewModel>();
            foreach (var gh in cartItems)
            {
                var product = db.Products.Find(gh.ProductId);
                if (product == null) continue;

                string variantInfo = gh.VariantInfo;
                var variant = gh.VariantId.HasValue ? db.ProductVariants.Find(gh.VariantId) : null;

                if (string.IsNullOrEmpty(variantInfo) && gh.VariantId.HasValue && variant != null)
                {
                    var vavs = db.VariantAttributeValues
                        .Include("Attribute")
                        .Include("AttributeValue")
                        .Where(vav => vav.VariantId == gh.VariantId)
                        .OrderBy(vav => vav.Attribute.DisplayOrder)
                        .ToList();

                    if (vavs.Any())
                    {
                        variantInfo = string.Join(", ", vavs
                            .Where(vav => vav.Attribute != null && vav.AttributeValue != null)
                            .Select(vav => vav.Attribute.Name + ": " + (vav.AttributeValue.DisplayValue ?? vav.AttributeValue.Value)));
                    }
                }

                if (string.IsNullOrEmpty(variantInfo))
                {
                    variantInfo = "Mặc định";
                }

                selectedCartItems.Add(new GHViewModel
                {
                    CartItemId = gh.GioHangId,
                    ProductId = gh.ProductId,
                    ProductName = product.Name,
                    VariantId = gh.VariantId,
                    VariantInfo = variantInfo,
                    Quantity = gh.Quantity,
                    Price = (int)(variant?.SalePrice ?? variant?.Price ?? product.Price ?? 0),
                    MaxQuantity = variant?.StockQuantity ?? product.Quantity ?? 0,
                    Photo = variant?.ImageVariant ?? product.Photo,
                    CreateDate = gh.CreateDate,
                    UpdateDate = gh.UpdateDate
                });
            }

            var user = db.Users.Find(session.UserId);
            var model = new CheckoutViewModel
            {
                ShipName = user.Name,
                ShipPhone = user.Phone.ToString(),
                ShipEmail = user.Email,
                ShipAddress = user.Address
            };

            ViewBag.CartItems = selectedCartItems;

            // Lưu danh sách sản phẩm đã chọn vào Session để xử lý khi POST
            Session["SelectedCartItems"] = selectedItems;

            // Chỉ định rõ render View "Checkout" thay vì tìm "Checkoutselected"
            return View("Checkout", model);
        }

        //[HttpPost]
        //
        //public ActionResult Checkout(CheckoutViewModel model, string paymentMethod, string Note)
        //{
        //    var session = (UserLogin)Session[Commoncontent.user_sesion];
        //    if (session == null) return Redirect("/dang-nhap");

        //    // Lấy danh sách sản phẩm đã chọn từ Session
        //    var selectedItems = Session["SelectedCartItems"] as int[];
        //    if (selectedItems == null || selectedItems.Length == 0)
        //    {
        //        TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng chọn lại sản phẩm!";
        //        return RedirectToAction("Index");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        // Lấy lại cart items để hiển thị
        //        var cartItemsForView = db.GioHang
        //            .Where(c => c.UserId == session.UserId && selectedItems.Contains(c.GioHangId))
        //            .ToList();
        //        ViewBag.CartItems = GetCartItemsViewModel(cartItemsForView);
        //        return View(model);
        //    }

        //    try
        //    {
        //        var order = new Order
        //        {
        //            UserId = session.UserId,
        //            ShipName = model.ShipName,
        //            ShipPhone = model.ShipPhone,
        //            ShipEmail = model.ShipEmail,
        //            ShipAddress = model.ShipAddress,
        //            CreatedDate = DateTime.Now,
        //            UpdateDate = DateTime.Now,
        //            StatusId = 1
        //        };

        //        int orderId = orderDao.Insert(order);

        //        // Lấy CHỈ những sản phẩm đã chọn từ giỏ hàng
        //        var cartItems = db.GioHang
        //            .Where(c => c.UserId == session.UserId && selectedItems.Contains(c.GioHangId))
        //            .ToList();

        //        decimal totalAmount = 0;

        //        foreach (var gh in cartItems)
        //        {
        //            var product = db.Products.Find(gh.ProductId);
        //            var variant = gh.VariantId.HasValue ? db.ProductVariants.Find(gh.VariantId) : null;

        //            int currentPrice = (int)(variant?.SalePrice ?? variant?.Price ?? product.Price ?? 0);
        //            totalAmount += currentPrice * gh.Quantity;

        //            var detail = new OrderDetail
        //            {
        //                OrderId = orderId,
        //                ProductId = gh.ProductId,
        //                VariantId = gh.VariantId,
        //                VariantInfo = gh.VariantInfo,
        //                Quantity = gh.Quantity,
        //                Price = currentPrice
        //            };
        //            orderDetailDao.Insert(detail);

        //            // Giảm số lượng tồn kho
        //            if (variant != null)
        //            {
        //                variant.StockQuantity -= gh.Quantity;
        //                if (variant.StockQuantity < 0) variant.StockQuantity = 0;
        //            }
        //            else if (product != null)
        //            {
        //                product.Quantity -= gh.Quantity;
        //                if (product.Quantity < 0) product.Quantity = 0;
        //            }
        //        }

        //        // XÓA CHỈ NHỮNG SẢN PHẨM ĐÃ THANH TOÁN khỏi giỏ hàng
        //        db.GioHang.RemoveRange(cartItems);
        //        db.SaveChanges();

        //        // Xóa Session
        //        Session.Remove("SelectedCartItems");

        //        // Gửi email
        //        try
        //        {
        //            SendOrderEmail(orderId, model, cartItems, totalAmount, paymentMethod, Note);
        //            TempData["EmailSuccess"] = "Email xác nhận đã được gửi thành công!";
        //        }
        //        catch (Exception emailEx)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"❌ Lỗi gửi email: {emailEx.Message}");
        //            TempData["EmailError"] = $"Đơn hàng đã được tạo nhưng không gửi được email: {emailEx.Message}";
        //        }

        //        return RedirectToAction("OrderSuccess", new { id = orderId });
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"❌ Lỗi checkout: {ex.Message}");
        //        ModelState.AddModelError("", "Lỗi: " + ex.Message);

        //        var cartItemsForView = db.GioHang
        //            .Where(c => c.UserId == session.UserId && selectedItems.Contains(c.GioHangId))
        //            .ToList();
        //        ViewBag.CartItems = GetCartItemsViewModel(cartItemsForView);
        //        return View("Checkout", model);
        //    }
        //}

        // Helper method để convert sang ViewModel
        private List<GHViewModel> GetCartItemsViewModel(List<GioHang> cartItems)
        {
            var list = new List<GHViewModel>();
            foreach (var gh in cartItems)
            {
                var product = db.Products.Find(gh.ProductId);
                if (product == null) continue;

                string variantInfo = gh.VariantInfo;
                var variant = gh.VariantId.HasValue ? db.ProductVariants.Find(gh.VariantId) : null;

                if (string.IsNullOrEmpty(variantInfo) && gh.VariantId.HasValue && variant != null)
                {
                    var vavs = db.VariantAttributeValues
                        .Include("Attribute")
                        .Include("AttributeValue")
                        .Where(vav => vav.VariantId == gh.VariantId)
                        .OrderBy(vav => vav.Attribute.DisplayOrder)
                        .ToList();

                    if (vavs.Any())
                    {
                        variantInfo = string.Join(", ", vavs
                            .Where(vav => vav.Attribute != null && vav.AttributeValue != null)
                            .Select(vav => vav.Attribute.Name + ": " + (vav.AttributeValue.DisplayValue ?? vav.AttributeValue.Value)));
                    }
                }

                if (string.IsNullOrEmpty(variantInfo))
                {
                    variantInfo = "Mặc định";
                }

                list.Add(new GHViewModel
                {
                    CartItemId = gh.GioHangId,
                    ProductId = gh.ProductId,
                    ProductName = product.Name,
                    VariantId = gh.VariantId,
                    VariantInfo = variantInfo,
                    Quantity = gh.Quantity,
                    Price = (int)(variant?.SalePrice ?? variant?.Price ?? product.Price ?? 0),
                    MaxQuantity = variant?.StockQuantity ?? product.Quantity ?? 0,
                    Photo = variant?.ImageVariant ?? product.Photo,
                    CreateDate = gh.CreateDate,
                    UpdateDate = gh.UpdateDate
                });
            }
            return list;
        }

        // ==============================
        // HÀM GỬI EMAIL
        // ==============================
        //        private void SendOrderEmail(int orderId, CheckoutViewModel model, List<GioHang> cartItems, decimal totalAmount, string paymentMethod, string note)
        //        {
        //            try
        //            {
        //                // Validate email trước khi gửi
        //                if (string.IsNullOrEmpty(model.ShipEmail))
        //                {
        //                    throw new Exception("Email người nhận không hợp lệ");
        //                }

        //                System.Diagnostics.Debug.WriteLine($"📧 Đang chuẩn bị email cho: {model.ShipEmail}");

        //                // Tạo bảng sản phẩm HTML
        //                StringBuilder productTable = new StringBuilder();
        //                productTable.Append("<table style='width:100%; border-collapse: collapse; margin: 20px 0;'>");
        //                productTable.Append("<thead><tr style='background: #8B4513; color: white;'>");
        //                productTable.Append("<th style='padding: 12px; text-align: left; border: 1px solid #ddd;'>Sản phẩm</th>");
        //                productTable.Append("<th style='padding: 12px; text-align: center; border: 1px solid #ddd;'>SL</th>");
        //                productTable.Append("<th style='padding: 12px; text-align: right; border: 1px solid #ddd;'>Đơn giá</th>");
        //                productTable.Append("<th style='padding: 12px; text-align: right; border: 1px solid #ddd;'>Thành tiền</th>");
        //                productTable.Append("</tr></thead><tbody>");

        //                foreach (var item in cartItems)
        //                {
        //                    var product = db.Products.Find(item.ProductId);
        //                    var variant = item.VariantId.HasValue ? db.ProductVariants.Find(item.VariantId) : null;
        //                    int price = (int)(variant?.SalePrice ?? variant?.Price ?? product.Price ?? 0);
        //                    int subtotal = price * item.Quantity;

        //                    productTable.Append("<tr style='border-bottom: 1px solid #ddd;'>");
        //                    productTable.Append($"<td style='padding: 12px;'><strong>{product.Name}</strong>");

        //                    if (!string.IsNullOrEmpty(item.VariantInfo) && item.VariantInfo != "Mặc định")
        //                    {
        //                        productTable.Append($"<br/><small style='color: #666;'>{item.VariantInfo}</small>");
        //                    }

        //                    productTable.Append("</td>");
        //                    productTable.Append($"<td style='padding: 12px; text-align: center;'>{item.Quantity}</td>");
        //                    productTable.Append($"<td style='padding: 12px; text-align: right;'>{price:N0}₫</td>");
        //                    productTable.Append($"<td style='padding: 12px; text-align: right; font-weight: bold; color: #ee4d2d;'>{subtotal:N0}₫</td>");
        //                    productTable.Append("</tr>");
        //                }

        //                productTable.Append("</tbody></table>");

        //                // Xử lý phương thức thanh toán
        //                string paymentMethodText = "💵 Thanh toán khi nhận hàng (COD)";
        //                if (paymentMethod == "bank") paymentMethodText = "🏦 Chuyển khoản ngân hàng";
        //                else if (paymentMethod == "momo") paymentMethodText = "📱 Ví điện tử MoMo";

        //                // Tạo nội dung email
        //                string emailBody = $@"
        //<!DOCTYPE html>
        //<html>
        //<head>
        //    <meta charset='utf-8'>
        //</head>
        //<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0;'>
        //    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background: #f9f9f9;'>
        //        <div style='background: linear-gradient(135deg, #8B4513 0%, #A0522D 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;'>
        //            <h1 style='margin: 0;'>CỬA HÀNG CÂY CẢNH ANH VŨ</h1>
        //            <p style='margin: 10px 0 0 0;'>Cảm ơn bạn đã đặt hàng!</p>
        //        </div>

        //        <div style='background: white; padding: 30px; border-radius: 0 0 8px 8px;'>
        //            <h2 style='color: #8B4513; border-bottom: 2px solid #8B4513; padding-bottom: 10px;'>
        //                Chi tiết đơn hàng #{orderId}
        //            </h2>

        //            <div style='background: #f8f9fa; padding: 15px; border-left: 4px solid #8B4513; margin: 20px 0;'>
        //                <p style='margin: 5px 0;'><strong>👤 Tên khách hàng:</strong> {model.ShipName}</p>
        //                <p style='margin: 5px 0;'><strong>📞 Điện thoại:</strong> {model.ShipPhone}</p>
        //                <p style='margin: 5px 0;'><strong>✉️ Email:</strong> {model.ShipEmail}</p>
        //                <p style='margin: 5px 0;'><strong>📍 Địa chỉ:</strong> {model.ShipAddress}</p>
        //                <p style='margin: 5px 0;'><strong>💳 Thanh toán:</strong> {paymentMethodText}</p>
        //                {(!string.IsNullOrEmpty(note) ? $"<p style='margin: 5px 0;'><strong>📝 Ghi chú:</strong> {note}</p>" : "")}
        //                <p style='margin: 5px 0;'><strong>🕐 Thời gian:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
        //            </div>

        //            <h3 style='color: #8B4513; margin-top: 30px;'>📦 Sản phẩm đã đặt</h3>
        //            {productTable}

        //            <div style='background: #fff5f5; padding: 20px; text-align: right; border-radius: 8px; margin-top: 20px;'>
        //                <p style='margin: 0 0 10px 0; font-size: 16px;'>Tổng thanh toán:</p>
        //                <p style='margin: 0; font-size: 24px; color: #ee4d2d; font-weight: bold;'>{totalAmount:N0}₫</p>
        //            </div>

        //            <div style='background: #e8f5e9; padding: 15px; border-radius: 8px; margin-top: 20px;'>
        //                <p style='margin: 0; color: #2e7d32;'><strong>✅ Đơn hàng đã được tiếp nhận!</strong></p>
        //                <p style='margin: 10px 0 0 0; font-size: 14px;'>Chúng tôi sẽ liên hệ với bạn sớm nhất để xác nhận.</p>
        //            </div>
        //        </div>

        //        <div style='text-align: center; padding: 20px; color: #666; font-size: 14px;'>
        //            <p style='margin: 5px 0;'><strong>📞 Hotline:</strong> 0964 155 923</p>
        //            <p style='margin: 5px 0;'><strong>✉️ Email:</strong> daoanhvu3001@gmail.com</p>
        //        </div>
        //    </div>
        //</body>
        //</html>";

        //                System.Diagnostics.Debug.WriteLine("📧 Đang tạo MailMessage...");

        //                // Tạo email message
        //                MailMessage mail = new MailMessage();
        //                mail.From = new MailAddress("daoanhvu3001@gmail.com", "Cây Cảnh Anh Vũ");
        //                mail.To.Add(model.ShipEmail);
        //                mail.Subject = $"Xác nhận đơn hàng #{orderId} - Cây Cảnh Anh Vũ";
        //                mail.Body = emailBody;
        //                mail.IsBodyHtml = true;
        //                mail.BodyEncoding = Encoding.UTF8;
        //                mail.SubjectEncoding = Encoding.UTF8;

        //                System.Diagnostics.Debug.WriteLine("📧 Đang cấu hình SMTP...");

        //                // Cấu hình SMTP
        //                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
        //                smtp.Credentials = new NetworkCredential("daoanhvu3001@gmail.com", "hiru wrwo jcrl emit");
        //                smtp.EnableSsl = true;
        //                smtp.Timeout = 30000; // 30 seconds timeout

        //                System.Diagnostics.Debug.WriteLine("📧 Đang gửi email...");

        //                // Gửi email
        //                smtp.Send(mail);

        //                System.Diagnostics.Debug.WriteLine($"✅ Email đã gửi thành công đến: {model.ShipEmail}");
        //            }
        //            catch (SmtpException smtpEx)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"❌ SMTP Error: {smtpEx.Message}");
        //                System.Diagnostics.Debug.WriteLine($"❌ Status Code: {smtpEx.StatusCode}");
        //                throw new Exception($"Lỗi SMTP: {smtpEx.Message} (Code: {smtpEx.StatusCode})");
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"❌ General Email Error: {ex.Message}");
        //                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
        //                throw new Exception($"Lỗi gửi email: {ex.Message}");
        //            }
        //        }
        // ==============================
        // HÀM GỬI EMAIL - ĐÃ SỬA
        // ==============================
        private void SendOrderEmail(int orderId, CheckoutViewModel model, string paymentMethod, string note)
        {
            try
            {
                // Validate email trước khi gửi
                if (string.IsNullOrEmpty(model.ShipEmail))
                {
                    throw new Exception("Email người nhận không hợp lệ");
                }

                System.Diagnostics.Debug.WriteLine($"📧 Đang chuẩn bị email cho: {model.ShipEmail}");

                // ✅ LẤY DỮ LIỆU TỪ ORDER & ORDERDETAIL ĐÃ LƯU
                var order = db.Orders.Find(orderId);
                var orderDetails = db.OrderDetails
                    .Where(od => od.OrderId == orderId)
                    .ToList();

                // Tạo bảng sản phẩm HTML
                StringBuilder productTable = new StringBuilder();
                productTable.Append("<table style='width:100%; border-collapse: collapse; margin: 20px 0;'>");
                productTable.Append("<thead><tr style='background: #8B4513; color: white;'>");
                productTable.Append("<th style='padding: 12px; text-align: left; border: 1px solid #ddd;'>Sản phẩm</th>");
                productTable.Append("<th style='padding: 12px; text-align: center; border: 1px solid #ddd;'>SL</th>");
                productTable.Append("<th style='padding: 12px; text-align: right; border: 1px solid #ddd;'>Đơn giá</th>");
                productTable.Append("<th style='padding: 12px; text-align: right; border: 1px solid #ddd;'>Thành tiền</th>");
                productTable.Append("</tr></thead><tbody>");

                decimal totalAmount = 0;

                // ✅ DUYỆT QUA ORDERDETAIL - GIÁ ĐÃ ĐÚNG TỪ LÚC LƯU
                foreach (var od in orderDetails)
                {
                    var product = db.Products.Find(od.ProductId);

                    // ✅ SỬ DỤNG GIÁ ĐÃ LƯU TRONG ORDERDETAIL
                    int price = (int)od.Price;
                    int quantity = (int)od.Quantity;
                    decimal subtotal = od.Price * od.Quantity ?? 0;
                    totalAmount += subtotal;

                    productTable.Append("<tr style='border-bottom: 1px solid #ddd;'>");
                    productTable.Append($"<td style='padding: 12px;'><strong>{product?.Name ?? "N/A"}</strong>");

                    if (!string.IsNullOrEmpty(od.VariantInfo) && od.VariantInfo != "Mặc định")
                    {
                        productTable.Append($"<br/><small style='color: #666;'>{od.VariantInfo}</small>");
                    }

                    productTable.Append("</td>");
                    productTable.Append($"<td style='padding: 12px; text-align: center;'>{quantity}</td>");
                    productTable.Append($"<td style='padding: 12px; text-align: right;'>{price:N0}₫</td>");
                    productTable.Append($"<td style='padding: 12px; text-align: right; font-weight: bold; color: #ee4d2d;'>{subtotal:N0}₫</td>");
                    productTable.Append("</tr>");
                }

                productTable.Append("</tbody></table>");

                // Xử lý phương thức thanh toán
                string paymentMethodText = "💵 Thanh toán khi nhận hàng (COD)";
                if (paymentMethod == "bank") paymentMethodText = "🏦 Chuyển khoản ngân hàng";
                else if (paymentMethod == "momo") paymentMethodText = "📱 Ví điện tử MoMo";

                // Tạo nội dung email
                string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background: #f9f9f9;'>
        <div style='background: linear-gradient(135deg, #8B4513 0%, #A0522D 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;'>
            <h1 style='margin: 0;'>CỬA HÀNG CÂY CẢNH ANH VŨ</h1>
            <p style='margin: 10px 0 0 0;'>Cảm ơn bạn đã đặt hàng!</p>
        </div>
        
        <div style='background: white; padding: 30px; border-radius: 0 0 8px 8px;'>
            <h2 style='color: #8B4513; border-bottom: 2px solid #8B4513; padding-bottom: 10px;'>
                Chi tiết đơn hàng #{orderId}
            </h2>
            
            <div style='background: #f8f9fa; padding: 15px; border-left: 4px solid #8B4513; margin: 20px 0;'>
                <p style='margin: 5px 0;'><strong>👤 Tên khách hàng:</strong> {model.ShipName}</p>
                <p style='margin: 5px 0;'><strong>📞 Điện thoại:</strong> {model.ShipPhone}</p>
                <p style='margin: 5px 0;'><strong>✉️ Email:</strong> {model.ShipEmail}</p>
                <p style='margin: 5px 0;'><strong>📍 Địa chỉ:</strong> {model.ShipAddress}</p>
                <p style='margin: 5px 0;'><strong>💳 Thanh toán:</strong> {paymentMethodText}</p>
                {(!string.IsNullOrEmpty(note) ? $"<p style='margin: 5px 0;'><strong>📝 Ghi chú:</strong> {note}</p>" : "")}
                <p style='margin: 5px 0;'><strong>🕐 Thời gian:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
            </div>

            <h3 style='color: #8B4513; margin-top: 30px;'>📦 Sản phẩm đã đặt</h3>
            {productTable}

            <div style='background: #fff5f5; padding: 20px; text-align: right; border-radius: 8px; margin-top: 20px;'>
                <p style='margin: 0 0 10px 0; font-size: 16px;'>Tổng thanh toán:</p>
                <p style='margin: 0; font-size: 24px; color: #ee4d2d; font-weight: bold;'>{totalAmount:N0}₫</p>
            </div>

            <div style='background: #e8f5e9; padding: 15px; border-radius: 8px; margin-top: 20px;'>
                <p style='margin: 0; color: #2e7d32;'><strong>✅ Đơn hàng đã được tiếp nhận!</strong></p>
                <p style='margin: 10px 0 0 0; font-size: 14px;'>Chúng tôi sẽ liên hệ với bạn sớm nhất để xác nhận.</p>
            </div>
        </div>

        <div style='text-align: center; padding: 20px; color: #666; font-size: 14px;'>
            <p style='margin: 5px 0;'><strong>📞 Hotline:</strong> 0964 155 923</p>
            <p style='margin: 5px 0;'><strong>✉️ Email:</strong> daoanhvu3001@gmail.com</p>
        </div>
    </div>
</body>
</html>";

                System.Diagnostics.Debug.WriteLine("📧 Đang tạo MailMessage...");

                // Tạo email message
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("daoanhvu3001@gmail.com", "Cây Cảnh Anh Vũ");
                mail.To.Add(model.ShipEmail);
                mail.Subject = $"Xác nhận đơn hàng #{orderId} - Cây Cảnh Anh Vũ";
                mail.Body = emailBody;
                mail.IsBodyHtml = true;
                mail.BodyEncoding = Encoding.UTF8;
                mail.SubjectEncoding = Encoding.UTF8;

                System.Diagnostics.Debug.WriteLine("📧 Đang cấu hình SMTP...");

                // Cấu hình SMTP
                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("daoanhvu3001@gmail.com", "hiru wrwo jcrl emit");
                smtp.EnableSsl = true;
                smtp.Timeout = 30000; // 30 seconds timeout

                System.Diagnostics.Debug.WriteLine("📧 Đang gửi email...");

                // Gửi email
                smtp.Send(mail);

                System.Diagnostics.Debug.WriteLine($"✅ Email đã gửi thành công đến: {model.ShipEmail}");
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SMTP Error: {smtpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Status Code: {smtpEx.StatusCode}");
                throw new Exception($"Lỗi SMTP: {smtpEx.Message} (Code: {smtpEx.StatusCode})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ General Email Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                throw new Exception($"Lỗi gửi email: {ex.Message}");
            }
        }
        // ==============================
        // TEST EMAIL (Dùng để debug)
        // ==============================
        public ActionResult TestEmail()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧪 Bắt đầu test email...");

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("daoanhvu3001@gmail.com", "Test Cây Cảnh Anh Vũ");
                mail.To.Add("daoanhvu3001@gmail.com"); // Gửi cho chính mình để test
                mail.Subject = "Test Email - " + DateTime.Now.ToString("HH:mm:ss");
                mail.Body = $@"
                    <h2>Email Test Thành Công!</h2>
                    <p>Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                    <p>Nếu nhận được email này, cấu hình SMTP đã hoạt động đúng.</p>
                ";
                mail.IsBodyHtml = true;
                mail.BodyEncoding = Encoding.UTF8;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("daoanhvu3001@gmail.com", "hiru wrwo jcrl emit");
                smtp.EnableSsl = true;
                smtp.Timeout = 30000;

                System.Diagnostics.Debug.WriteLine("🧪 Đang gửi test email...");
                smtp.Send(mail);
                System.Diagnostics.Debug.WriteLine("✅ Test email đã gửi!");

                return Content(@"
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body style='font-family: Arial; padding: 50px; text-align: center;'>
                        <h2 style='color: green;'>✅ Email test đã gửi thành công!</h2>
                        <p>Kiểm tra hộp thư: daoanhvu3001@gmail.com</p>
                        <p><a href='/Cart/Index'>← Quay lại giỏ hàng</a></p>
                    </body>
                    </html>
                ", "text/html");
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SMTP Test Error: {smtpEx.Message}");
                return Content($@"
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body style='font-family: Arial; padding: 50px;'>
                        <h2 style='color: red;'>❌ Lỗi SMTP</h2>
                        <p><strong>Message:</strong> {smtpEx.Message}</p>
                        <p><strong>Status Code:</strong> {smtpEx.StatusCode}</p>
                        <hr>
                        <pre>{smtpEx.StackTrace}</pre>
                        <p><a href='/Cart/Index'>← Quay lại</a></p>
                    </body>
                    </html>
                ", "text/html");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Test Error: {ex.Message}");
                return Content($@"
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body style='font-family: Arial; padding: 50px;'>
                        <h2 style='color: red;'>❌ Lỗi: {ex.Message}</h2>
                        <hr>
                        <pre>{ex.StackTrace}</pre>
                        <p><a href='/Cart/Index'>← Quay lại</a></p>
                    </body>
                    </html>
                ", "text/html");
            }
        }

        // ==============================
        // HIỂN THỊ ĐẶT HÀNG THÀNH CÔNG
        // ==============================
        public ActionResult OrderSuccess(int id)
        {
            try
            {
                var order = db.Orders.Find(id);
                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Index", "Home");
                }

                var orderDetails = (from od in db.OrderDetails
                                    join p in db.Products on od.ProductId equals p.ProductId
                                    where od.OrderId == id
                                    select new OrderDetailViewModel
                                    {
                                        ProductName = p.Name,
                                        VariantInfo = od.VariantInfo ?? "Mặc định",
                                        Quantity = (int)od.Quantity,
                                        Price = (int)od.Price,
                                        Total = od.Price * od.Quantity ?? 0
                                    }).ToList();

                var model = new OrderSuccessViewModel
                {
                    Order = order,
                    OrderDetails = orderDetails,
                    Total = orderDetails.Sum(x => x.Total)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OrderSuccess: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // ==============================
        // HELPER
        // ==============================
        private List<GHViewModel> GetCartItems(int userId)
        {
            var cartItems = db.GioHang.Where(c => c.UserId == userId).ToList();
            var list = new List<GHViewModel>();

            foreach (var gh in cartItems)
            {
                var product = db.Products.Find(gh.ProductId);
                if (product == null) continue;

                string variantInfo = gh.VariantInfo;
                var variant = gh.VariantId.HasValue ? db.ProductVariants.Find(gh.VariantId) : null;

                if (string.IsNullOrEmpty(variantInfo) && gh.VariantId.HasValue && variant != null)
                {
                    var vavs = db.VariantAttributeValues
                        .Include("Attribute")
                        .Include("AttributeValue")
                        .Where(vav => vav.VariantId == gh.VariantId)
                        .OrderBy(vav => vav.Attribute.DisplayOrder)
                        .ToList();

                    if (vavs.Any())
                    {
                        variantInfo = string.Join(", ", vavs
                            .Where(vav => vav.Attribute != null && vav.AttributeValue != null)
                            .Select(vav => vav.Attribute.Name + ": " + (vav.AttributeValue.DisplayValue ?? vav.AttributeValue.Value)));
                    }
                }

                if (string.IsNullOrEmpty(variantInfo))
                {
                    variantInfo = "Mặc định";
                }

                list.Add(new GHViewModel
                {
                    CartItemId = gh.GioHangId,
                    ProductId = gh.ProductId,
                    ProductName = product.Name,
                    VariantId = gh.VariantId,
                    VariantInfo = variantInfo,
                    Quantity = gh.Quantity,
                    Price = (int)(variant?.SalePrice ?? variant?.Price ?? product.Price ?? 0),
                    MaxQuantity = variant?.StockQuantity ?? product.Quantity ?? 0,
                    Photo = variant?.ImageVariant ?? product.Photo,
                    CreateDate = gh.CreateDate,
                    UpdateDate = gh.UpdateDate
                });
            }

            return list;
        }
        //[HttpPost]
        //
        //public ActionResult Checkout(CheckoutViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var session = (UserLogin)Session[Commoncontent.user_sesion];
        //        var cart = Session[Commoncontent.CartSession] as List<CartItem>; // Lấy giỏ hàng từ Session

        //        if (cart != null && cart.Any())
        //        {
        //            // 1. LƯU ĐƠN HÀNG (ORDER)
        //            var order = new Order();
        //            order.CreatedDate = DateTime.Now;
        //            order.ShipName = model.ShipName;
        //            order.ShipPhone = model.ShipPhone;
        //            order.ShipAddress = model.ShipAddress;
        //            order.ShipEmail = model.ShipEmail; // Quan trọng để gửi mail
        //            order.StatusId = 1; // Mới tạo
        //            order.UserId = session.UserId;

        //            db.Orders.Add(order);
        //            db.SaveChanges(); // Lưu để lấy OrderID

        //            // 2. LƯU CHI TIẾT ĐƠN HÀNG (ORDER DETAILS)
        //            decimal totalAmount = 0;
        //            foreach (var item in cart)
        //            {
        //                var orderDetail = new OrderDetail();
        //                orderDetail.OrderId = order.OrderId;
        //                orderDetail.ProductId = item.ProductId;
        //                orderDetail.Price = item.Product.Price;
        //                orderDetail.Quantity = item.Quantity;

        //                db.OrderDetails.Add(orderDetail);
        //                totalAmount += (item.Product.Price.GetValueOrDefault(0) * item.Quantity);
        //            }
        //            db.SaveChanges();

        //            // 3. GỬI EMAIL XÁC NHẬN (Phần bạn đang thiếu)
        //            try
        //            {
        //                // Đọc file template email (tạo file này ở bước 2)
        //                string content = System.IO.File.ReadAllText(Server.MapPath("~/Assets/client/template/neworder.html"));

        //                // Thay thế các biến trong template
        //                content = content.Replace("{{CustomerName}}", model.ShipName);
        //                content = content.Replace("{{Phone}}", model.ShipPhone);
        //                content = content.Replace("{{Email}}", model.ShipEmail);
        //                content = content.Replace("{{Address}}", model.ShipAddress);
        //                content = content.Replace("{{OrderId}}", order.OrderId.ToString());
        //                content = content.Replace("{{Total}}", totalAmount.ToString("N0"));

        //                // Gửi mail (Hàm gửi mail bạn đã test thành công)
        //                // Giả sử bạn có class MailHelper
        //                new MailHelper().SendMail(model.ShipEmail, "Đơn hàng mới từ Website Nội Thất", content);
        //            }
        //            catch (Exception ex)
        //            {
        //                // Ghi log lỗi gửi mail nhưng không chặn quy trình đặt hàng
        //                // System.Diagnostics.Debug.WriteLine("Lỗi gửi mail: " + ex.Message);
        //            }

        //            // 4. XÓA GIỎ HÀNG VÀ CHUYỂN HƯỚNG
        //            Session[Commoncontent.CartSession] = null;
        //            return RedirectToAction("Success", "Cart");
        //        }
        //    }
        //    return View(model);
        //}




        // ==============================
        // LỊCH SỬ ĐƠN HÀNG
        // ==============================
        public ActionResult HistoryCart()
        {
            try
            {
                var session = (UserLogin)Session[Commoncontent.user_sesion];

                // Debug: Kiểm tra session
                System.Diagnostics.Debug.WriteLine("=== HistoryCart ===");
                System.Diagnostics.Debug.WriteLine($"Session: {(session != null ? "OK" : "NULL")}");

                if (session == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Session NULL - Redirect to login");
                    return Redirect("/dang-nhap");
                }

                System.Diagnostics.Debug.WriteLine($"UserId: {session.UserId}");

                // Lấy đơn hàng
                var orders = db.Orders
                    .Where(o => o.UserId == session.UserId)
                    .OrderByDescending(o => o.CreatedDate ?? DateTime.MinValue)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Số đơn hàng tìm thấy: {orders.Count}");

                // Debug: In thông tin từng đơn
                foreach (var order in orders)
                {
                    System.Diagnostics.Debug.WriteLine($"  Order #{order.OrderId}, Status: {order.StatusId}, Date: {order.CreatedDate}");
                }

                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LỖI HistoryCart: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // ==============================
        // CHI TIẾT ĐƠN HÀNG
        // ==============================
        public ActionResult OrderDetail(int id)
        {
            try
            {
                var session = (UserLogin)Session[Commoncontent.user_sesion];

                System.Diagnostics.Debug.WriteLine($"=== OrderDetail: {id} ===");
                System.Diagnostics.Debug.WriteLine($"Session: {(session != null ? "OK" : "NULL")}");

                if (session == null) return Redirect("/dang-nhap");

                System.Diagnostics.Debug.WriteLine($"UserId: {session.UserId}");

                // Kiểm tra đơn hàng
                var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == session.UserId);

                System.Diagnostics.Debug.WriteLine($"Order found: {(order != null ? "YES" : "NO")}");

                if (order == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Order not found or unauthorized");
                    TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này.";
                    return RedirectToAction("HistoryCart");
                }

                // Lấy chi tiết
                var orderDetails = (from od in db.OrderDetails
                                    join p in db.Products on od.ProductId equals p.ProductId
                                    where od.OrderId == id
                                    select new OrderDetailViewModel
                                    {
                                        ProductName = p.Name,
                                        VariantInfo = od.VariantInfo ?? "Mặc định",
                                        Quantity = (int)od.Quantity,
                                        Price = (int)od.Price,
                                        Total = od.Price * od.Quantity ?? 0
                                    }).ToList();

                System.Diagnostics.Debug.WriteLine($"Số sản phẩm: {orderDetails.Count}");

                var model = new OrderSuccessViewModel
                {
                    Order = order,
                    OrderDetails = orderDetails,
                    Total = orderDetails.Sum(x => x.Total)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LỖI OrderDetail: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("HistoryCart");
            }
        }

        // ==============================
        // HỦY ĐƠN HÀNG
        // ==============================
        [HttpPost]
       
        public ActionResult CancelOrder(int id)
        {
            try
            {
                var session = (UserLogin)Session[Commoncontent.user_sesion];
                if (session == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });

                // ✅ Kiểm tra đơn hàng thuộc user
                var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == session.UserId);
                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                // ✅ Chỉ cho phép hủy đơn hàng "Đã tiếp nhận" (StatusId = 1)
                if (order.StatusId != 1)
                {
                    return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng đang chờ xử lý" });
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // ✅ Hoàn lại số lượng tồn kho
                        var orderDetails = db.OrderDetails.Where(od => od.OrderId == id).ToList();
                        foreach (var od in orderDetails)
                        {
                            if (od.VariantId.HasValue)
                            {
                                var variant = db.ProductVariants.Find(od.VariantId);
                                if (variant != null)
                                {
                                    variant.StockQuantity += od.Quantity;
                                    System.Diagnostics.Debug.WriteLine($"✅ Hoàn lại variant {od.VariantId}: +{od.Quantity}");
                                }
                            }
                            else
                            {
                                var product = db.Products.Find(od.ProductId);
                                if (product != null)
                                {
                                    product.Quantity += od.Quantity;
                                    System.Diagnostics.Debug.WriteLine($"✅ Hoàn lại product {od.ProductId}: +{od.Quantity}");
                                }
                            }
                        }

                        // ✅ Cập nhật trạng thái đơn hàng
                        order.StatusId = 5; // 5 = Đã hủy
                        order.UpdateDate = DateTime.Now;
                        db.SaveChanges();

                        transaction.Commit();

                        System.Diagnostics.Debug.WriteLine($"✅ Đã hủy đơn hàng #{id}");
                        return Json(new { success = true, message = "Hủy đơn hàng thành công" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"❌ Lỗi hủy đơn: {ex.Message}");
                        return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi CancelOrder: {ex.Message}");
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}