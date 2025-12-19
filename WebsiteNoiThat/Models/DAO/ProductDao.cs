using System;
using System.Collections.Generic;
using System.Linq;
using Models.EF;
using Models.ViewModels;

namespace Models.DAO
{
    public class ProductDao
    {
        DBNoiThat db = null;

        public ProductDao()
        {
            db = new DBNoiThat();
        }

        // ✅ Lấy danh sách sản phẩm (CHỈ còn bán cho Client)
        public List<Product> ListSanPham()
        {
            return db.Products.Where(p => p.IsActive).ToList();
        }

        // ✅ Lấy sản phẩm mới (CHỈ còn bán)
        public List<Product> NewProduct()
        {
            return db.Products
                .Where(n => n.IsActive && (n.Discount == 0 || n.EndDate < DateTime.Now || n.StartDate > DateTime.Now))
                .OrderByDescending(n => n.StartDate)
                .Take(8)
                .ToList();
        }

        // ✅ Lấy sản phẩm giảm giá (CHỈ còn bán)
        public List<Product> SaleProduct()
        {
            return db.Products
                .Where(n => n.IsActive && n.Discount > 0)
                .OrderByDescending(n => n.Discount)
                .Take(8)
                .ToList();
        }

        // ✅ Lấy sản phẩm bán chạy (Hot) - CHỈ còn bán
        public List<ProductView> ProductHot()
        {
            var model = (from a in db.Products
                         join b in db.OrderDetails on a.ProductId equals b.ProductId
                         where a.IsActive // ✅ Chỉ lấy sản phẩm còn bán
                         group b by new { a.ProductId, a.Name, a.Photo, a.Price, a.Discount, a.StartDate, a.EndDate, a.Description }
                         into g
                         select new ProductView
                         {
                             ProductId = g.Key.ProductId,
                             Name = g.Key.Name,
                             Photo = g.Key.Photo,
                             Price = g.Key.Price,
                             Discount = g.Key.Discount,
                             StartDate = g.Key.StartDate,
                             EndDate = g.Key.EndDate,
                             Description = g.Key.Description,
                             Quantity = g.Sum(s => s.Quantity)
                         })
                         .OrderByDescending(n => n.Quantity)
                         .Take(6)
                         .ToList();
            return model;
        }

        // ✅ Xem chi tiết sản phẩm (Client) - CHỈ còn bán
        public Product DetailsProduct(int id)
        {
            return db.Products.FirstOrDefault(p => p.ProductId == id && p.IsActive);
        }

        // ✅ Lấy danh sách biến thể của sản phẩm (CHỈ còn bán)
        public List<ProductVariantViewModel> GetVariants(int productId)
        {
            var variants = (from pv in db.ProductVariants
                            join p in db.Products on pv.ProductId equals p.ProductId
                            where pv.ProductId == productId
                                  && pv.IsActive == true
                                  && p.IsActive == true // ✅ Sản phẩm phải còn bán
                            select new ProductVariantViewModel
                            {
                                VariantId = pv.VariantId,
                                SKU = pv.SKU,
                                Price = pv.Price,
                                SalePrice = pv.SalePrice,
                                StockQuantity = pv.StockQuantity,
                                Image = pv.ImageVariant,
                                AttributesInfo = (from vav in db.VariantAttributeValues
                                                  join av in db.AttributeValues on vav.ValueId equals av.ValueId
                                                  join a in db.Attributes on av.AttributeId equals a.AttributeId
                                                  where vav.VariantId == pv.VariantId
                                                  orderby a.DisplayOrder
                                                  select a.Name + ": " + av.DisplayValue).ToList()
                            }).ToList();

            foreach (var item in variants)
            {
                item.VariantName = item.AttributesInfo != null && item.AttributesInfo.Any()
                    ? string.Join(", ", item.AttributesInfo)
                    : item.SKU;
            }

            return variants;
        }

        // ✅ Thêm sản phẩm mới (Admin - Mặc định IsActive = true)
        public long Insert(Product entity, List<ProductVariant> variants = null, List<VariantAttributeValue> variantAttributes = null)
        {
            try
            {
                entity.IsActive = true; // ✅ Mặc định là còn bán
                db.Products.Add(entity);
                db.SaveChanges();

                if (variants != null && variants.Count > 0)
                {
                    foreach (var variant in variants)
                    {
                        variant.ProductId = entity.ProductId;
                        db.ProductVariants.Add(variant);
                        db.SaveChanges();
                    }
                }
                return entity.ProductId;
            }
            catch
            {
                return 0;
            }
        }

        // ✅ Cập nhật sản phẩm
        public bool Update(Product entity)
        {
            try
            {
                var product = db.Products.Find(entity.ProductId);
                if (product != null)
                {
                    product.Name = entity.Name;
                    product.Description = entity.Description;
                    product.Price = entity.Price;
                    product.Quantity = entity.Quantity;
                    product.CateId = entity.CateId;
                    product.Photo = entity.Photo;
                    product.StartDate = entity.StartDate;
                    product.EndDate = entity.EndDate;
                    product.Discount = entity.Discount;
                    product.IsActive = entity.IsActive; // ✅ Cập nhật trạng thái

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

        // ✅ Tìm theo danh mục (CHỈ còn bán)
        public List<Product> ListByCategoryId(int cateId, ref int total, int pageindex = 1, int pagesize = 12)
        {
            total = db.Products.Where(x => x.CateId == cateId && x.IsActive).Count();
            return db.Products
                .Where(x => x.CateId == cateId && x.IsActive)
                .OrderByDescending(x => x.Price)
                .Skip((pageindex - 1) * pagesize)
                .Take(pagesize)
                .ToList();
        }

        // ✅ Tìm kiếm (CHỈ còn bán)
        public List<Product> Search(string keyword, ref int total, int pageindex = 1, int pagesize = 12)
        {
            total = db.Products.Where(x => x.Name.Contains(keyword) && x.IsActive).Count();
            return db.Products
                .Where(n => n.Name.Contains(keyword) && n.IsActive)
                .OrderByDescending(x => x.Price)
                .Skip((pageindex - 1) * pagesize)
                .Take(pagesize)
                .ToList();
        }

        // ✅ Tìm tên sản phẩm (CHỈ còn bán)
        public List<string> ListName(string keyword)
        {
            return db.Products
                .Where(n => n.Name.Contains(keyword) && n.IsActive)
                .Select(n => n.Name)
                .Distinct()
                .ToList();
        }

        // ✅ XÓA MỀM: Chỉ cập nhật IsActive = false
        public bool Delete(int id)
        {
            try
            {
                var product = db.Products.Find(id);
                if (product != null)
                {
                    product.IsActive = false; // ✅ Xóa mềm
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

        // ✅ THÊM: Khôi phục sản phẩm
        public bool Restore(int id)
        {
            try
            {
                var product = db.Products.Find(id);
                if (product != null)
                {
                    product.IsActive = true;
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
    }
}