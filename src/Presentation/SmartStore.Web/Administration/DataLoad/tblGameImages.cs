using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartStore.Admin.DataLoad
{
    public class tblGameImages
    {
        public int ID { get; set; }
        public int refGamesID { get; set; }
        public int nOrder { get; set; }
        public string strImageType { get; set; }
        public string strFileName { get; set; }
        public int nWidth { get; set; }
        public int nHeight { get; set; }
    }
}