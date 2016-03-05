using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telerik.Web.Mvc;

namespace SmartStore.Admin.Models.ReferenceGames
{
    public class ReferenceGamesListModel
    {
        public string Name { get; set; }

        public GridModel<ReferenceGame> Games { get; set; }
    }
}