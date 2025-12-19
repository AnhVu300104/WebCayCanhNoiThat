namespace Models.EF
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class DBNoiThat : DbContext
    {
        public DBNoiThat() : base("name=DBNoiThat")
        {
        }

        // Các bảng cũ
        public virtual DbSet<Card> Cards { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Contact> Contacts { get; set; }
        public virtual DbSet<Credential> Credentials { get; set; }
        public virtual DbSet<GioHang> GioHang { get; set; }
        public virtual DbSet<News> News { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Provider> Providers { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Status> Status { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserGroup> UserGroups { get; set; }
        public virtual DbSet<ChatMessage> ChatMessages { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }

        // --- CÁC BẢNG MỚI THÊM (Attribute & Variant) ---
        public virtual DbSet<Attribute> Attributes { get; set; }
        public virtual DbSet<AttributeValue> AttributeValues { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<VariantAttributeValue> VariantAttributeValues { get; set; }

        // --- CÁC BẢNG MỚI THÊM (Kho hàng - Warehouse & Stock) ---
        public virtual DbSet<Warehouse> Warehouses { get; set; }
        public virtual DbSet<WarehouseStock> WarehouseStocks { get; set; }

        // --- CÁC BẢNG MỚI THÊM (Nhập/Xuất/Kiểm kho) ---
        public virtual DbSet<StockIn> StockIns { get; set; }
        public virtual DbSet<StockInDetail> StockInDetails { get; set; }
        public virtual DbSet<StockOut> StockOuts { get; set; }
        public virtual DbSet<StockOutDetail> StockOutDetails { get; set; }
        public virtual DbSet<StockCheck> StockChecks { get; set; }
        public virtual DbSet<StockCheckDetail> StockCheckDetails { get; set; }
        public virtual DbSet<StockHistory> StockHistories { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Cấu hình Fluent API nếu cần thiết (ví dụ: độ chính xác cho decimal)
            modelBuilder.Entity<ProductVariant>()
                .Property(e => e.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProductVariant>()
                .Property(e => e.SalePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<StockIn>()
                .Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<StockInDetail>()
                .Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<StockInDetail>()
                .Property(e => e.TotalPrice)
                .HasPrecision(18, 2);
        }
    }
}