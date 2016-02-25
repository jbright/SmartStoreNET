using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public class ReferenceGame
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public string Description { get; set; }
        public int EntryId { get; set; }
        public int Year { get; set; }
        public string Manufacturer { get; set; }
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
