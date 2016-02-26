using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartStore.Admin.DataLoad
{
    public class tblRefGameEntryImages
    {
        public int ID { get; set; }
        public int refRefGameEntryID { get; set; }
        public int nOrder { get; set; }
        public string strImageType { get; set; }
        public ImageType ImageType { get; set; }
        public string strDescription { get; set; }
        public string strUrl { get; set; }
        public int nWidth { get; set; }
        public int nHeight { get; set; }
        public int bIsProfile { get; set; }
    }
}