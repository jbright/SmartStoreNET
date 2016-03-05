using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Domain.Seo;
using SmartStore.Data;
using SmartStore.Services.Catalog;
using SmartStore.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmartStore.Web.Controllers
{
    public class RedirectorController : PublicControllerBase
    {

        #region Fields
        private readonly IManufacturerService ManufacturerService;
        private readonly IProductService ProductService;
        #endregion

        #region Constructors

        public RedirectorController(
            IManufacturerService manufacturerService,
            IProductService productService)
        {
            this.ManufacturerService = manufacturerService;
            this.ProductService = productService;
        }

        #endregion

        public ActionResult HomePage()
        {
            return RedirectPermanent("/");
        }

        public ActionResult Newest()
        {
            return RedirectPermanent("/newproducts");
        }

        // GET: Redirector
        public ActionResult Product(int id)
        {
            var product = ProductService.GetProductBySku(id.ToString());
            if (product == null)
                return Redirect("/");

            // We have an item. find the url.
            var context = new SmartObjectContext("EC");
            var url = FindUrl(context, product.Id, "Product");
            if (url == null)
                return Redirect("/");

            return RedirectPermanent("/" + url.Slug);
        }

        public ActionResult Category(string c)
        {
            return RedirectPermanent("/" + c.ToLower().Replace(".", "-").Replace("all-", ""));
        }

        public ActionResult Manufacturer(string man)
        {
            var manf = GetAll(ManufacturerService).Find(m => String.Compare(m.Name, man, true) == 0);
            if (manf == null)
                return Redirect("/");
            var context = new SmartObjectContext("EC");
            var url = FindUrl(context, manf.Id, "Manufacturer");
            if (url == null)
                return Redirect("/");

            return RedirectPermanent("/" + url.Slug);
        }

        private static List<Manufacturer> GetAll(IManufacturerService manufacturerService)
        {
            if (_ms == null)
            {
                lock (_ms_lock)
                {
                    if (_ms == null)
                        _ms = manufacturerService.GetAllManufacturers().ToList();
                }
            }
            return _ms;
        }
        private static List<Manufacturer> _ms;
        private static object _ms_lock = new object();

        private static UrlRecord FindUrl(SmartObjectContext context, int entityId, string entityName)
        {
            return context
                    .Set<UrlRecord>()
                    .Where(u => u.EntityName == entityName && u.EntityId == entityId)
                    .FirstOrDefault();
        }

    }
}