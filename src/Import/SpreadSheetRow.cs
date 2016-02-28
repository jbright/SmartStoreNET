using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public class SpreadSheetRow
    {
        public int SKU { get; set;  }

        public decimal Weight { get; set; }

        public decimal AdditionalShipPrice { get; set;  }

        public string SeoTitle { get; set; }
    }

    /// <summary>
    /// Handles the mapping from the standard format.
    /// </summary>
    public class SpreadSheetRowMap : CsvClassMap<SpreadSheetRow>
    {
        public SpreadSheetRowMap()
        {
            Map(m => m.SKU).Name("SKU");
            Map(m => m.Weight).Name("Weight");
            Map(m => m.AdditionalShipPrice).Name("Additional Ship Price");
            Map(m => m.SeoTitle).Name("SEO Title");
        }
    }
}
