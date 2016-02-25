using Insight.Database;
using SmartStore.Services.Catalog;
using SmartStore.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmartStore.Web.Controllers
{
    public class ReferenceGameController : PublicControllerBase
    {
        private readonly CatalogHelper _helper;
        private readonly IProductService _productService;

        #region Constructors
        public ReferenceGameController(
			IProductService productService,
			CatalogHelper helper)
        {
			this._productService = productService;
			this._helper = helper;
        }
        
        #endregion

        // GET: ReferenceGame
        [OutputCache(Duration = 60 * 60, VaryByParam = "productId")]
        public ActionResult Index(int productId)
        {
            ReferenceGameModel model = null;

            var product = _productService.GetProductById(productId);
            if (product == null)
                return View(model);

            var attributes = _helper.PrepareProductSpecificationModel(product);
            if (attributes == null)
                return View(model);

            var attribute = attributes.ToList().Find(a => a.SpecificationAttributeName == "Related Game Title");
            if (attribute == null)
                return View(model);

            var repo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString)).As<ReferenceGameRepository>());
            var game = repo.GetByName(attribute.SpecificationAttributeOption);
            if (game == null)
                return View(model);

            model = new ReferenceGameModel()
            {
                Game = game,
                Pictures = repo.GetGamePictures(game.Id),
            };

            return View(model);
        }

    }

    public class ReferenceGameModel
    {
        public ReferenceGame Game { get; set; }
        public List<ReferenceGamePicture> Pictures { get; set; }
    }

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

    public class ReferenceGamePicture
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public interface ReferenceGameRepository
    {
        [Sql("SELECT * FROM QA_ReferenceGame WHERE Name=@name")]
        ReferenceGame GetByName(string name);

        [Sql("SELECT * FROM QA_ReferenceGamePicture WHERE ReferenceGameId=@id")]
        List<ReferenceGamePicture> GetGamePictures(int id);
    }
}