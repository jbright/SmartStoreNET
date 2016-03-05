using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmartStore.Admin.Models.ReferenceGames
{
    public class ReferenceGame
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }

        [AllowHtml]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public int EntryId { get; set; }

        public int Year { get; set; }
        public string Manufacturer { get; set; }

        [AllowHtml]
        [DataType(DataType.MultilineText)]
        public string AdditionalDescription { get; set; }
        public string MonitorSync { get; set; }
        public string MonitorComposite { get; set; }
        public string MonitorOrientation { get; set; }
        public string MonitorResolution { get; set; }
        public string MonitorType { get; set; }
        public string ConversionType { get; set; }
        public string NumberPlayers { get; set; }
    }

}