using System;
using System.Linq;
using System.Web.Mvc;
using SmartStore.Admin.Models.Catalog;
using SmartStore.Collections;
using SmartStore.Core;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Discounts;
using SmartStore.Core.Events;
using SmartStore.Core.Logging;
using SmartStore.Services.Catalog;
using SmartStore.Services.Common;
using SmartStore.Services.Customers;
using SmartStore.Services.Discounts;
using SmartStore.Services.Filter;
using SmartStore.Services.Helpers;
using SmartStore.Services.Localization;
using SmartStore.Services.Media;
using SmartStore.Services.Security;
using SmartStore.Services.Seo;
using SmartStore.Services.Stores;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Filters;
using SmartStore.Web.Framework.Modelling;
using SmartStore.Web.Framework.Security;
using Telerik.Web.Mvc;
using Telerik.Web.Mvc.UI;
using SmartStore.Admin.DataLoad;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartStore.Core.Infrastructure;
using System.IO;
using System.Net;
using SmartStore.Core.IO;
using SmartStore.Data;

namespace SmartStore.Admin.Controllers
{
    [AdminAuthorize]
    public class DataController : AdminControllerBase
    {
        #region Fields

        private readonly ICategoryService _categoryService;
        private readonly ICategoryTemplateService _categoryTemplateService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IPictureService _pictureService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;
        private readonly IAclService _aclService;
        private readonly IStoreService _storeService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly IEventPublisher _eventPublisher;
        private readonly IFilterService _filterService;

        #endregion

		#region Constructors
        public DataController(ICategoryService categoryService, ICategoryTemplateService categoryTemplateService,
            IManufacturerService manufacturerService, IProductService productService, 
            ICustomerService customerService,
            IUrlRecordService urlRecordService, IPictureService pictureService, ILanguageService languageService,
            ILocalizationService localizationService, ILocalizedEntityService localizedEntityService,
            IDiscountService discountService, IPermissionService permissionService,
			IAclService aclService, IStoreService storeService, IStoreMappingService storeMappingService,
            IWorkContext workContext,
            ICustomerActivityService customerActivityService,
			IDateTimeHelper dateTimeHelper,
			AdminAreaSettings adminAreaSettings,
            CatalogSettings catalogSettings,
            IEventPublisher eventPublisher, 
			IFilterService filterService)
        {
            this._categoryService = categoryService;
            this._categoryTemplateService = categoryTemplateService;
            this._manufacturerService = manufacturerService;
            this._productService = productService;
            this._customerService = customerService;
            this._urlRecordService = urlRecordService;
            this._pictureService = pictureService;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._localizedEntityService = localizedEntityService;
            this._discountService = discountService;
            this._permissionService = permissionService;
            this._aclService = aclService;
			this._storeService = storeService;
			this._storeMappingService = storeMappingService;
            this._workContext = workContext;
            this._customerActivityService = customerActivityService;
			this._dateTimeHelper = dateTimeHelper;
            this._adminAreaSettings = adminAreaSettings;
            this._catalogSettings = catalogSettings;
			this._eventPublisher = eventPublisher;
            this._filterService = filterService;
        }

        #endregion

        // GET: Data
        public ActionResult Index(string message)
        {
            ViewBag.Message = message;
            return View();
        }

        private static IDictionary<Guid, CategoryLoader> CategoryLoaderTasks = new Dictionary<Guid, CategoryLoader>();

        [ActionName("import-categories")]
        public ActionResult ImportCategories()
        {
            var id = Guid.NewGuid();
            var loader = EngineContext.Current.Resolve<CategoryLoader>();
            CategoryLoaderTasks.Add(id, loader);

            Task.Factory.StartNew(() =>
            {
                loader.Start();
                CategoryLoaderTasks.Remove(id);
            });

            return Json(id);
        }

        [ActionName("import-categories-status")]
        public ActionResult ImportCategoriesStatus(Guid id)
        {
            if (CategoryLoaderTasks.Keys.Contains(id))
                return Json(CategoryLoaderTasks[id].Progress, JsonRequestBehavior.AllowGet);
            else
                return Json("", JsonRequestBehavior.AllowGet);
        }

        [ActionName("import-pictures")]
        public ActionResult ImportPictures(int id)
        {
            // return Json("OK");

            var product = _productService.GetProductById(id);
            StringBuilder sb = new StringBuilder();

            if (product != null)
            {
                sb.AppendFormat("Successfully loaded item {0} with Id {1}", product.Name, product.Id);
                sb.AppendLine();

                var legacyGame = LegacyRepo.GameRepo.GetById(Int32.Parse(product.Sku));
                if (legacyGame != null)
                {
                    sb.AppendFormat("Legacy game found with Id {0}", legacyGame.ID);
                    sb.AppendLine();

                    var images = LegacyRepo.GameImageRepo.GetAllForGame(legacyGame.ID);
                    if (images != null)
                    {
                        sb.AppendFormat("Found {0} image(s)", images.Count);
                        sb.AppendLine();
                        images.ForEach(img =>
                            {
                                sb.AppendLine("  image: " + img.strFileName);

                                string remoteImageUrl = "http://www.quarterarcade.com" + img.strFileName;
                                string localName = Guid.NewGuid().ToString().Replace("-", "");
                                string localFileName = localName + Path.GetExtension(remoteImageUrl);

                                WebClient webClient = new WebClient();
                                string fullPath = Server.MapPath("~/Staging/") + localFileName;
                                webClient.DownloadFile(remoteImageUrl, fullPath);

                                sb.AppendLine("  downloaded to : " + localName);

                                var picture = _pictureService.InsertPicture(System.IO.File.ReadAllBytes(fullPath), ContentType(Path.GetExtension(remoteImageUrl)), null, true, false);
                                sb.AppendLine("  picture record with Id : " + picture.Id);

                                var productPicture = new ProductPicture
                                {
                                    PictureId = picture.Id,
                                    ProductId = product.Id,
                                };

                                MediaHelper.UpdatePictureTransientStateFor(productPicture, pp => pp.PictureId);

                                _productService.InsertProductPicture(productPicture);

                                _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(product.Name));

                                System.IO.File.Delete(fullPath);
                                Console.WriteLine("  picture associated with product!");
                            });

                    }
                    else
                    {
                        sb.AppendLine("No images found.");
                    }

                    var sampleImages = LegacyRepo.ReferenceImagesRepo.GetSampleImagesForGame(legacyGame.ID);
                    if (sampleImages != null)
                    {
                        sb.AppendFormat("Found {0} sampleimage(s)", sampleImages.Count);
                        sb.AppendLine();
                        sampleImages.ForEach(img =>
                        {
                            string burl = img.strUrl.Replace("_th_", "_fs_");
                            sb.AppendLine("  image: " + burl);

                            // http://ggdb.com/img/ggdb/vol1/1827_1_fs_pb.jpg
                            string remoteImageUrl = "http://ggdb.com" + burl;
                            string localName = Guid.NewGuid().ToString().Replace("-", "");
                            string localFileName = localName + Path.GetExtension(remoteImageUrl);
                            // hack to fix missing image files.
                            if (!localFileName.EndsWith(".jpg"))
                                localFileName = localFileName + ".jpg";

                            WebClient webClient = new WebClient();
                            string fullPath = Server.MapPath("~/Staging/") + localFileName;
                            webClient.DownloadFile(remoteImageUrl, fullPath);

                            sb.AppendLine("  downloaded to : " + localName);

                            try
                            {
                                string fileExtension = Path.GetExtension(remoteImageUrl);
                                if (String.IsNullOrWhiteSpace(fileExtension))
                                    fileExtension = ".jpg";

                                var picture = _pictureService.InsertPicture(System.IO.File.ReadAllBytes(fullPath), ContentType(fileExtension), null, true, false);
                                sb.AppendLine("  picture record with Id : " + picture.Id);

                                var productPicture = new ProductPicture
                                {
                                    PictureId = picture.Id,
                                    ProductId = product.Id,
                                };

                                MediaHelper.UpdatePictureTransientStateFor(productPicture, pp => pp.PictureId);

                                _productService.InsertProductPicture(productPicture);

                                _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(product.Name));

                                System.IO.File.Delete(fullPath);
                                Console.WriteLine("  picture associated with product!");

                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine("  PICTURE COULD NOT BE PROCESSED!!");
                                sb.AppendLine(ex.ToString());
                            }
                        });

                    }
                    else
                    {
                        sb.AppendLine("No sample images found.");
                    }

                    // product.MetaTitle = "IMAGES PROCESSED";

                    var Context = new SmartObjectContext("EC");

                    var attribute = Context.Set<SpecificationAttribute>().Where(a => a.Name == "Image Processed").FirstOrDefault();
                    var falseOption = Context
                        .Set<SpecificationAttributeOption>()
                        .Where(o => o.SpecificationAttributeId == attribute.Id && o.Name == false.ToString())
                        .FirstOrDefault();
                    var trueOption = Context
                        .Set<SpecificationAttributeOption>()
                        .Where(o => o.SpecificationAttributeId == attribute.Id && o.Name == true.ToString())
                        .FirstOrDefault();
                    var productAttribute = Context.Set<ProductSpecificationAttribute>()
                        .Where(a => a.ProductId == product.Id && a.SpecificationAttributeOptionId == falseOption.Id)
                        .FirstOrDefault();
                    if (productAttribute != null)
                    {
                        productAttribute.SpecificationAttributeOptionId = trueOption.Id;
                        Context.Set<SpecificationAttributeOption>();
                        Context.SaveChanges();
                    }

                    _productService.UpdateProduct(product, false);
                }
                else
                    sb.AppendLine("Legacy Game not found!");

                return Json(sb.ToString());
            }
            return Json("Could not find item: " + id.ToString());
        }

        public string ContentType(string extension)
        {
            var contentType = MimeTypes.MapNameToMimeType(extension);
            if (contentType != null)
                return contentType;

            switch (extension.ToLowerInvariant())
            {
                case ".bmp":
                    contentType = "image/bmp";
                    break;
                case ".gif":
                    contentType = "image/gif";
                    break;
                case ".jpeg":
                case ".jpg":
                case ".jpe":
                case ".jfif":
                case ".pjpeg":
                case ".pjp":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                case ".tiff":
                case ".tif":
                    contentType = "image/tiff";
                    break;
                case ".svg":
                    contentType = "image/svg+xml";
                    break;
                case ".ico":
                    contentType = "image/x-icon";
                    break;
                default:
                    break;
            }

            return contentType;
        }

        [ActionName("get-item-list")]
        public ActionResult GetListToProcess(int count = 100)
        {
            var Context = new SmartObjectContext("EC");

            var attribute = Context.Set<SpecificationAttribute>().Where(a => a.Name == "Image Processed").FirstOrDefault();
            var option = Context
                .Set<SpecificationAttributeOption>()
                .Where(o => o.SpecificationAttributeId == attribute.Id && o.Name == false.ToString())
                .FirstOrDefault();

            var toProcess = Context
                .Set<ProductSpecificationAttribute>()
                .Where(p => p.SpecificationAttributeOptionId == option.Id)
                .Take(count);

            var ids = new List<int>();
            toProcess.ToList().ForEach(i =>
                ids.Add(i.ProductId));
            return Json(ids);
        }

        [ActionName("get-all-categories")]
        public ActionResult GetAllCategories()
        {
            var cats = _categoryService.GetAllCategories();
            var urls = new List<string>();
            cats.ToList().ForEach(i =>
                urls.Add(i.GetSeName()));
            return Json(urls);
        }

        [ActionName("get-all-category-ids")]
        public ActionResult GetAllCategoryId()
        {
            var cats = _categoryService.GetAllCategories();
            var urls = new List<int>();
            cats.ToList().ForEach(i =>
                urls.Add(i.Id));
            return Json(urls);
        }

        [ActionName("import-category-pictures")]
        public ActionResult ImportCategoryPictures(int id)
        {
            // return Json("OK");

            var category = _categoryService.GetCategoryById(id);
            StringBuilder sb = new StringBuilder();

            if (category != null)
            {
                sb.AppendFormat("Successfully loaded category {0} with Id {1}", category.Name, category.Id);
                sb.AppendLine();

                var seo = category.GetSeName();
                string fullPath = Server.MapPath("~/Staging/Categories/" + seo + ".jpg");
                if (System.IO.File.Exists(fullPath))
                {
                    sb.AppendFormat("  Found {0} image", fullPath);
                    sb.AppendLine();
                    var picture = _pictureService.InsertPicture(System.IO.File.ReadAllBytes(fullPath), ContentType(Path.GetExtension(fullPath)), null, true, false);
                    sb.AppendLine("  picture record with Id : " + picture.Id);

                    MediaHelper.UpdatePictureTransientStateFor(category, c => c.PictureId);
                    category.PictureId = picture.Id;
                    _categoryService.UpdateCategory(category);
                    _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(category.Name));
                    sb.AppendLine("  DONE!");
                }
                else
                {
                    sb.AppendLine("  No picture available. Skipping.");
                    sb.AppendLine();
                }

                return Json(sb.ToString());
            }
            return Json("Could not find item: " + id.ToString());
        }
    }
}