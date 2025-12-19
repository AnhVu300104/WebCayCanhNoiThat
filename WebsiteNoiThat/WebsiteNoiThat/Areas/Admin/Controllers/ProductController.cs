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
using System.Drawing;
using System.Drawing.Imaging;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class ProductController : HomeController
    {
        DBNoiThat db = new DBNoiThat();
        private const int MaxFileSizeInMB = 5;

        [HasCredential(RoleId = "VIEW_PRODUCT")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            // Lấy TẤT CẢ sản phẩm (cả active và inactive) để hiển thị trạng thái
            //var productViewModels = (from a in db.Products
            //                         join c in db.Categories on a.CateId equals c.CategoryId into cateGroup
            //                         from c in cateGroup.DefaultIfEmpty()
            //                         select new ProductViewModel
            //                         {
            //                             ProductId = a.ProductId,
            //                             Name = a.Name,
            //                             Description = a.Description,
            //                             Discount = a.Discount,
            //                             CateName = c != null ? c.Name : "N/A",
            //                             Price = a.Price ?? 0,
            //                             Quantity = a.Quantity ?? 0,
            //                             StartDate = a.StartDate,
            //                             EndDate = a.EndDate,
            //                             Photo = a.Photo,
            //                             IsActive = a.IsActive // ✅ Lấy trạng thái
            //                         })
            //                         .OrderByDescending(x => x.IsActive) // Hiển thị sản phẩm còn bán trước
            //                         .ThenByDescending(x => x.ProductId)
            //                         .ToList();
            var productViewModels = (from a in db.Products
                                     join c in db.Categories on a.CateId equals c.CategoryId into cateGroup
                                     from c in cateGroup.DefaultIfEmpty()
                                     select new ProductViewModel
                                     {
                                         ProductId = a.ProductId,
                                         Name = a.Name,
                                         Description = a.Description,
                                         Discount = a.Discount,
                                         CateName = c != null ? c.Name : "N/A",
                                         Price = a.Price ?? 0,
                                         // ✅ Lấy tổng số lượng từ ProductVariant
                                         Quantity = db.ProductVariants
    .Where(v => v.ProductId == a.ProductId)
    .Sum(v => (int?)v.StockQuantity) ?? 0,

                                         StartDate = a.StartDate,
                                         EndDate = a.EndDate,
                                         Photo = a.Photo,
                                         IsActive = a.IsActive
                                     })
                         .OrderByDescending(x => x.IsActive)
                         .ThenBy(x => x.ProductId)
                         .ToList();


            return View(productViewModels);
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_PRODUCT")]
        public ActionResult Add()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name");

            return View();
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_PRODUCT")]
        [ValidateInput(false)]
        public ActionResult Add(ProductViewModel n, HttpPostedFileBase UploadImage)
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name", n.CateId);

            if (UploadImage != null && UploadImage.ContentLength > 0)
            {
                double fileSizeInMB = (double)UploadImage.ContentLength / (1024 * 1024);
                if (fileSizeInMB > MaxFileSizeInMB)
                {
                    ModelState.AddModelError("Photo", $"Kích thước ảnh không được vượt quá {MaxFileSizeInMB}MB. Ảnh của bạn: {fileSizeInMB:F2}MB");
                    return View(n);
                }

                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                string fileExtension = Path.GetExtension(UploadImage.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Photo", "Chỉ chấp nhận file ảnh: JPG, JPEG, PNG, GIF, BMP");
                    return View(n);
                }
            }

            if (!n.CateId.HasValue || n.CateId.Value == 0)
            {
                ModelState.AddModelError("CateId", "Vui lòng chọn danh mục sản phẩm");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToList();

                ViewBag.ErrorDetail = string.Join("<br/>", errors);
                return View(n);
            }

            if (n.EndDate.HasValue && n.StartDate.HasValue && n.EndDate < n.StartDate)
            {
                ModelState.AddModelError("ErrorDate", "Ngày kết thúc phải muộn hơn ngày bắt đầu.");
                return View(n);
            }

            try
            {
                var model = new Product
                {
                    Name = n.Name?.Trim(),
                    Description = n.Description?.Trim(),
                    CateId = n.CateId,
                    Price = n.Price ?? 0,
                    Quantity = 0, // Mặc định 0
                    StartDate = n.StartDate,
                    EndDate = n.EndDate,
                    Discount = n.Discount ?? 0,
                    IsActive = true // ✅ Mặc định là còn bán
                };

                if (UploadImage != null && UploadImage.ContentLength > 0)
                {
                    string fileName = SaveAndCompressImage(UploadImage);
                    model.Photo = fileName;
                }
                else
                {
                    model.Photo = "no-image.png";
                }

                db.Products.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Thêm sản phẩm thành công! Số lượng kho mặc định là 0.";
                return RedirectToAction("Show");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");

                var fullErrorMessage = string.Join("; ", errorMessages);
                ModelState.AddModelError("", "Lỗi validation từ database: " + fullErrorMessage);
                return View(n);
            }
            catch (Exception ex)
            {
                string errorMsg = "Lỗi thêm sản phẩm: " + ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += " | Chi tiết: " + ex.InnerException.Message;
                }

                ModelState.AddModelError("", errorMsg);
                return View(n);
            }
        }

        private string SaveAndCompressImage(HttpPostedFileBase file)
        {
            try
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName).ToLower();
                string uploadPath = Server.MapPath("~/image");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string fullPath = Path.Combine(uploadPath, fileName);

                if (file.ContentLength > 1024 * 1024)
                {
                    using (var image = Image.FromStream(file.InputStream))
                    {
                        int maxWidth = 1200;
                        int maxHeight = 1200;

                        int newWidth = image.Width;
                        int newHeight = image.Height;

                        if (image.Width > maxWidth || image.Height > maxHeight)
                        {
                            double ratioX = (double)maxWidth / image.Width;
                            double ratioY = (double)maxHeight / image.Height;
                            double ratio = Math.Min(ratioX, ratioY);

                            newWidth = (int)(image.Width * ratio);
                            newHeight = (int)(image.Height * ratio);
                        }

                        using (var newImage = new Bitmap(newWidth, newHeight))
                        {
                            using (var graphics = Graphics.FromImage(newImage))
                            {
                                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                            }

                            var encoderParameters = new EncoderParameters(1);
                            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);

                            ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                            if (jpegCodec != null)
                            {
                                newImage.Save(fullPath, jpegCodec, encoderParameters);
                            }
                            else
                            {
                                newImage.Save(fullPath, ImageFormat.Jpeg);
                            }
                        }
                    }
                }
                else
                {
                    file.SaveAs(fullPath);
                }

                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lưu ảnh: " + ex.Message);
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        [HttpGet]
        [HasCredential(RoleId = "EDIT_PRODUCT")]
        public ActionResult Edit(int ProductId)
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            var product = db.Products.Find(ProductId);
            if (product == null)
            {
                return HttpNotFound();
            }

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Discount = product.Discount,
                Price = product.Price ?? 0,
                Quantity = product.Quantity ?? 0,
                StartDate = product.StartDate,
                EndDate = product.EndDate,
                Photo = product.Photo,
                CateId = product.CateId,
                IsActive = product.IsActive // ✅ Lấy trạng thái
            };

            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name", product.CateId);

            return View(model);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_PRODUCT")]
        [ValidateInput(false)]
        public ActionResult Edit(ProductViewModel n, HttpPostedFileBase UploadImage)
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name", n.CateId);

            if (UploadImage != null && UploadImage.ContentLength > 0)
            {
                double fileSizeInMB = (double)UploadImage.ContentLength / (1024 * 1024);
                if (fileSizeInMB > MaxFileSizeInMB)
                {
                    ModelState.AddModelError("Photo", $"Kích thước ảnh không được vượt quá {MaxFileSizeInMB}MB. Ảnh của bạn: {fileSizeInMB:F2}MB");
                    return View(n);
                }

                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                string fileExtension = Path.GetExtension(UploadImage.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Photo", "Chỉ chấp nhận file ảnh: JPG, JPEG, PNG, GIF, BMP");
                    return View(n);
                }
            }

            if (!n.CateId.HasValue || n.CateId.Value == 0)
            {
                ModelState.AddModelError("CateId", "Vui lòng chọn danh mục sản phẩm");
            }

            if (!ModelState.IsValid)
            {
                return View(n);
            }

            if (n.EndDate.HasValue && n.StartDate.HasValue && n.EndDate < n.StartDate)
            {
                ModelState.AddModelError("ErrorDate", "Ngày kết thúc phải muộn hơn ngày bắt đầu");
                return View(n);
            }

            try
            {
                var model = db.Products.Find(n.ProductId);
                if (model == null) return HttpNotFound();

                model.Name = n.Name?.Trim();
                model.Description = n.Description?.Trim();
                model.CateId = n.CateId;
                model.Price = n.Price ?? 0;
                model.Quantity = n.Quantity ?? 0;
                model.StartDate = n.StartDate;
                model.EndDate = n.EndDate;
                model.Discount = n.Discount ?? 0;
                model.IsActive = n.IsActive; // ✅ Cập nhật trạng thái

                if (UploadImage != null && UploadImage.ContentLength > 0)
                {
                    if (!string.IsNullOrEmpty(model.Photo) && model.Photo != "no-image.png")
                    {
                        string oldPath = Server.MapPath("~/image/" + model.Photo);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    string fileName = SaveAndCompressImage(UploadImage);
                    model.Photo = fileName;
                }

                db.SaveChanges();
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Show");
            }
            catch (Exception ex)
            {
                string errorMsg = "Lỗi cập nhật sản phẩm: " + ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += " | Chi tiết: " + ex.InnerException.Message;
                }

                ModelState.AddModelError("", errorMsg);
                return View(n);
            }
        }

        [HttpGet]
        [HasCredential(RoleId = "DELETE_PRODUCT")]
        public ActionResult Delete(int id)
        {
            try
            {
                var model = db.Products.Find(id);
                if (model != null)
                {
                    // ✅ XÓA MỀM: Chỉ cập nhật IsActive = false
                    model.IsActive = false;
                    db.SaveChanges();

                    TempData["Success"] = "Ngừng bán sản phẩm thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi cập nhật trạng thái sản phẩm: " + ex.Message;
            }

            return RedirectToAction("Show");
        }

        // ✅ THÊM ACTION MỚI: Khôi phục sản phẩm
        [HttpGet]
        [HasCredential(RoleId = "EDIT_PRODUCT")]
        public ActionResult Restore(int id)
        {
            try
            {
                var model = db.Products.Find(id);
                if (model != null)
                {
                    model.IsActive = true;
                    db.SaveChanges();

                    TempData["Success"] = "Khôi phục sản phẩm thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi khôi phục sản phẩm: " + ex.Message;
            }

            return RedirectToAction("Show");
        }

        public ActionResult Menu()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            var model = new CategoryDao().ListCategory();
            return PartialView(model);
        }
    }
}