using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebsiteNoiThat.Models
{
    public class StatusStatsViewModel
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public int Count { get; set; }
    }
}