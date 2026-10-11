using System;
using System.Collections.Generic;
using System.Text;

namespace UoR.Sys.LMS.UoRLearn.Models
{
    public class PagedResponse
    {
        public int totalResults { get; set; }
        public bool hasMoreResults { get; set; }
        public int startIndex { get; set; }
        public int itemsPerPage { get; set; }
        public List<LMSBase> results { get; set; }
    }
}
