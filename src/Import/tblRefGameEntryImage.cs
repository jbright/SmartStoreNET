using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public class tblRefGameEntryImage
    {
        public int Id { get; set; }
        public int refRefGameEntryID { get; set; }
        public int nOrder { get; set; }
        public string strImageType { get; set; }
        public string strDescription { get; set; }
        public int nWidth { get; set; }
        public int nHeight { get; set; }
        public string strName { get; set; }
        public string strUrl { get; set; }
    }
}
