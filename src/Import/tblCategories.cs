using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public class tblCategories
    {
        public string CatID { get; set; }
        public string strName { get; set; }
        public int nOrder { get; set; }
        public string strRightContent { get; set; }
        public string strParentCatID { get; set; }
        public string strPageTitle { get; set; }
        public string strParentArray { get; set; }
        public string strParentCntSQL { get; set; }
        public string strParentSQL { get; set; }
        public string strSQL { get; set; }
        public string strRevLookup1 { get; set; }
        public string strRevLookup2 { get; set; }
        public string strRevLookup3 { get; set; }
        public bool bRevLookup4 { get; set; }
        public bool bRevLookup5 { get; set; }
        public string strIcon { get; set; }
        public int nRecordsPerPage { get; set; }
        public string strNSIcon { get; set; }
        public string strNSTitle { get; set; }
        public string strNSText { get; set; }
    }
}
