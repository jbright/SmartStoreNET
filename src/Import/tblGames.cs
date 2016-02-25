using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public enum ItemStatus
    {
        Available = 1,
        eBay = 2,
        PreAuction = 3,
        Private = 4,
        Shopped = 5,
        Sold = 6,
        Unavailable = 7,
    }

    public class tblGames
    {
        public int ID { get; set; }
        public string refCatID { get; set; }
        public int refRefGameEntryID { get; set; }
        public bool bSpecialOrder { get; set; }
        public decimal mSpecialLowPrice { get; set; }
        public decimal mSpecialHighPrice { get; set; }
        public string strName { get; set; }
        public string strType { get; set; }
        public string strPartType { get; set; }
        public string strSubPartType { get; set; }
        public string strMeasureType { get; set; }
        public string strMonitor { get; set; }
        public ItemStatus strState { get; set; }
        public bool bNewEquip { get; set; }
        public decimal mSellPrice { get; set; }
        public decimal mShipping { get; set; }
        public string strEBayURL { get; set; }
        public string strEBayTitle { get; set; }
        public string strShortDescription { get; set; }
        public string strEBayState { get; set; }
        public string strPrivateNotes { get; set; }
        public string strLongDescription { get; set; }
        public string strThumb { get; set; }
        public bool bHasPic { get; set; }
        public int nNumInStock { get; set; }
        public decimal mPricePaid { get; set; }
        public int nRating { get; set; }
        public string strEBayHistory { get; set; }
        public DateTime dtCreated { get; set; }
        public DateTime dtUpdated { get; set; }
        public DateTime dtListed { get; set; }
        public bool bNewMonitor { get; set; }
        public bool bOfferFastSlow { get; set; }
        public bool bMultiPac { get; set; }
        public bool bDDK { get; set; }
        public bool bMultipede { get; set; }
        public bool bFeatured { get; set; }
        public string strOwner { get; set; }
        public float dblWeight { get; set; }
        public string strLocation { get; set; }
        public string strWorking { get; set; }


        // JOINED DATA
        public string strClass { get; set; }
        public string strSubClass { get; set; }
        public int nYear { get; set; }
        public string strManufacturer { get; set; }
        public string RefGameName { get; set; }
    }
}
