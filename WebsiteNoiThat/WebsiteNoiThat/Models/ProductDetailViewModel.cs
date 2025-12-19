using Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebsiteNoiThat.Models
{
    public class ProductDetailViewModel : ProductViewModel
    {
        
            public List<ProductVariantViewModel> Variants { get; set; }
        
    }
}