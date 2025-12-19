using System;
using System.Collections.Generic;
using Order = Models.EF.Order;
namespace WebsiteNoiThat.Models
{
    public class OrderDetailViewModel
    {
        public string ProductName { get; set; }
        public string VariantInfo { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Total { get; set; }
    }

    public class OrderSuccessViewModel
    {
        public Order Order { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
        public int Total { get; set; }
    }
}