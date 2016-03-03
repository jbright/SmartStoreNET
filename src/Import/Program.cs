﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Insight.Database;

using SmartStore.Data;
using SmartStore.Services.Seo;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Domain.Media;
using SmartStore.Core.Caching;
using SmartStore.Core.Domain.Seo;
using SmartStore.Core.Infrastructure;

using System.Data.SqlClient;
using System.Configuration;
using Autofac;
using System.Text.RegularExpressions;
using System.Net;
using CsvHelper;
using System.IO;

/*

-- cleanup:
delete from Product_SpecificationAttribute_Mapping
delete from SpecificationAttribute
delete from SpecificationAttributeOption
delete from Picture WHERE ID IN (SELECT PictureId FROM Product_Picture_Mapping);
delete from Category;
delete from Product;
delete from Manufacturer;
delete from UrlRecord where EntityName='Category';
delete from UrlRecord where EntityName='Product';
delete from UrlRecord where EntityName='Manufacturer';



 
 */
namespace Import
{
    class Program
    {
        private static SmartObjectContext Context;
        private static SpecificationAttribute LegacyIdAttribute;
        private static SpecificationAttribute LegacyReferenceGameIdAttribute;
        private static SpecificationAttribute LegacyReferenceGameAttribute;
        private static SpecificationAttribute ImageProcessedAttribute;
        private static SpecificationAttribute WorkingAttribute;

        private static SpecificationAttribute BoardTypeAttribute;


        private static SpecificationAttribute GameYearAttribute;
        private static SpecificationAttribute GameGenreAttribute;

        private static SpecificationAttribute ConditionAttribute;
        private static List<SpecificationAttributeOption> Conditions = new List<SpecificationAttributeOption>();

        static void Main(string[] args)
        {
            Context = new SmartObjectContext("EC");
            //TestCategory();

            // Already did -- don't need to do again.
            //ImportDidYouKnow();

            // Already did -- don't need to do again.
            //ImportReferenceGames();
            //ImportReferenceGameImages();

            SetupAttributes();
            PopulateNewAttributes();
            //PrepopulateReferenceGameAttributeValues();
            //PopulateManufacturers();
            //PopulateCategories();
            //PopulateProducts(10000);
            //BindProductToCategories();

            //SyncProducts();

            Console.WriteLine();
            Console.Write("DONE! Press ANY Key > ");
            Console.ReadKey();
        }

        private static void ImportReferenceGameImages()
        {
            Console.Write("Getting all screen images... ");
            var allImages = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ReferenceGameRepo>()).GetAllValidScreenCaptures();
            Console.WriteLine(" done!");

            WebClient webClient = new WebClient();
            var target = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString)).As<ReferenceGameRepo>());
            int counter = 0;
            int directory = 0;

            allImages.ForEach(image =>
                {
                    // First step is to see if we have this in our db
                    var existingGame = target.GetByName(image.strName);
                    if (existingGame != null)
                    {
                        var game = target.GetByName(image.strName);
                        if (game != null)
                        {
                            string localFileName = String.Format("{0:0000}", counter);
                            Console.Write("Downloading {0} -> {1}..", image.strName, localFileName);

                            //string fullPath = @"C:\SmartStore\build\Web\Media\ReferenceGames\0\" + localFileName + ".jpg";
                            string fullPath = @"C:\inetpub\wwwroot\SmartStore\Media\ReferenceGames\0\" + localFileName + ".jpg";

                            string urlPath = @"/Media/ReferenceGames/0/" + localFileName + ".jpg";

                            // /img/ggdb/vol1/1_1_fs_sc.jpg
                            string remoteImageUrl = "http://ggdb.com" + image.strUrl;
                            webClient.DownloadFile(remoteImageUrl, fullPath);
                            Console.Write(" done! ");

                            counter++;

                            // Save the record here.
                            target.AddReferencePicture(game.Id, urlPath, image.strDescription, image.nWidth, image.nHeight);

                            Console.WriteLine(" Added to DB!");
                        }
                        else
                            Console.WriteLine("Missing reference game {0}", image.strName);
                    }
                    else if (existingGame == null)
                    {
                        Console.WriteLine("Missing game {0}", image.strName);
                    }
                });
        }

        private static void SetupAttributes()
        {
            LegacyIdAttribute = SetupSpecificationAttributes("Legacy ID");
            LegacyReferenceGameIdAttribute = SetupSpecificationAttributes("Legacy Reference Game ID");
            LegacyReferenceGameAttribute = SetupSpecificationAttributes("Related Game Title");
            ImageProcessedAttribute = SetupSpecificationAttributes("Image Processed");
            WorkingAttribute = SetupSpecificationAttributes("Working?");

            BoardTypeAttribute = SetupSpecificationAttributes("Board Type");

            ConditionAttribute = SetupSpecificationAttributes("Condition");

            Conditions.Add(AddOrFindValue(ConditionAttribute, "0 Stars", 10));     // 0
            Conditions.Add(AddOrFindValue(ConditionAttribute, "½ Stars", 9));      // 1
            Conditions.Add(AddOrFindValue(ConditionAttribute, "1 Stars", 8));      // 2
            Conditions.Add(AddOrFindValue(ConditionAttribute, "1 ½ Stars", 7));    // 3
            Conditions.Add(AddOrFindValue(ConditionAttribute, "2 Stars", 6));      // 4
            Conditions.Add(AddOrFindValue(ConditionAttribute, "2 ½ Stars", 5));    // 5
            Conditions.Add(AddOrFindValue(ConditionAttribute, "3 Stars", 4));      // 6
            Conditions.Add(AddOrFindValue(ConditionAttribute, "3 ½ Stars", 3));    // 7
            Conditions.Add(AddOrFindValue(ConditionAttribute, "4 Stars", 2));      // 8
            Conditions.Add(AddOrFindValue(ConditionAttribute, "4 ½ Stars", 1));    // 9
            Conditions.Add(AddOrFindValue(ConditionAttribute, "5 Stars", 0));      // 10

            AddOrFindValue(ImageProcessedAttribute, true.ToString());
            AddOrFindValue(ImageProcessedAttribute, false.ToString());

            AddOrFindValue(WorkingAttribute, "Untested");
            AddOrFindValue(WorkingAttribute, "Not Working");
            AddOrFindValue(WorkingAttribute, "Working");

            GameYearAttribute = SetupSpecificationAttributes("Year");
            for (int year = 1900; year <= 2020; year++)
                Conditions.Add(AddOrFindValue(GameYearAttribute, year.ToString()));

            GameGenreAttribute = SetupSpecificationAttributes("Genre");
            
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Simulator"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Fighting"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "System"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Role Playing Game"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Card"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Shooter"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Other"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Labyrinth/Maze"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Boardgame"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Mahjong"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Gambling"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Puzzle"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Ball and Paddle"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Platform"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Trivia"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Scrolling Shooter"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Space"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Pinball"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Scrolling Fighter"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Billiards/Pool"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Racing"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Skill"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Adventure"));
            Conditions.Add(AddOrFindValue(GameGenreAttribute, "Sports"));

        }

        private static void PrepopulateReferenceGameAttributeValues()
        {
            var repo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ReferenceGameRepo>());

            repo
                .GetAllValid()
                .ForEach(referenceGame =>
                {
                    AddOrFindValue(LegacyReferenceGameAttribute, referenceGame.Name);
                    Console.WriteLine("  Added {0} Game Reference Attribute Value", referenceGame.Name);
                });
        }

        private static void ImportDidYouKnow()
        {
            var repo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblDidYouKnow>());
            var target = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString)).As<ItblDidYouKnow>());
            var all = repo.GetAll();
            all.ForEach(quote =>
                {
                    target.Add(quote.strDYK);
                });
        }

        private static void ImportReferenceGames()
        {
            var repo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ReferenceGameRepo>());
            var target = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString)).As<ReferenceGameRepo>());

            repo
                .GetAllValid()
                .ForEach(referenceGame =>
                {
                    var existingGame = target.GetByName(referenceGame.Name);
                    if (existingGame != null && existingGame.Id == referenceGame.Id)
                    {
                        // do nothing
                        Console.WriteLine(" Skipping {0}", referenceGame.Name);
                    }
                    else if (existingGame == null)
                    {
                        // add it
                        target.Add(referenceGame);
                        Console.WriteLine(" Added {0}", referenceGame.Name);
                    }
                    else
                    {
                        // merge
                        bool changed = false;
                        if (String.IsNullOrWhiteSpace(existingGame.AdditionalDescription))
                        {
                            changed = true;
                            existingGame.AdditionalDescription = referenceGame.AdditionalDescription;
                        }

                        if (changed)
                        {
                            Console.WriteLine(" Merged {0}", referenceGame.Name);
                            target.Update(existingGame);
                        }
                        else
                        {
                            Console.WriteLine(" Ignored {0}", referenceGame.Name);
                        }
                    }
                });
        }

        private static void PopulateNewAttributes()
        {
            var repo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString)).As<ReferenceGameRepository>());
            var allItems = Context.Set<Product>().ToList();
            //var allProductSpecificationAttributes = Context.Set<ProductSpecificationAttribute>().ToList();
            //var allSpecificationOptions = Context.Set<SpecificationAttributeOption>().ToList();

            allItems.ForEach(product =>
                {
                    var option = repo.GetProductOption(LegacyReferenceGameAttribute.Id, product.Id);
                    if (option != null && !String.IsNullOrWhiteSpace(option.Name))
                    {
                        var game = repo.GetByName(option.Name);
                        if (game != null)
                        {
                            Console.Write("Updating {0} ... ", product.Name);
                            if (game.Year > 0)
                                AddAttributeAssociation(product, GameYearAttribute, game.Year.ToString(), true, true);
                            if (!String.IsNullOrWhiteSpace(game.Genre))
                                AddAttributeAssociation(product, GameGenreAttribute, game.Genre, true, true);
                            Console.WriteLine("done!");
                        }
                    }
                });
        }

        private static SpecificationAttribute SetupSpecificationAttributes(string name)
        {
            Console.Write("Setting up " + name + " Attribute ... ");

            var attributes = Context.Set<SpecificationAttribute>().ToList();

            Console.WriteLine("got all attributes.");
            var attribute = attributes.Find(a => a.Name == name);

            if (attribute == null)
            {
                Console.Write("Attribute not found. Creating ... ");
                attribute = new SpecificationAttribute()
                {
                    Name = name,
                    DisplayOrder = 0,
                };

                Context.Set<SpecificationAttribute>().Add(attribute);
                Context.SaveChanges();

                Console.WriteLine("Done!");
            }
            else
            {
                Console.WriteLine("Attribute found.");
            }

            return attribute;
        }

        private static SpecificationAttributeOption FindValue(SpecificationAttribute attribute, string value)
        {
            return Context
                        .Set<SpecificationAttributeOption>()
                        .Where(v => v.SpecificationAttributeId == attribute.Id && v.Name == value)
                        .FirstOrDefault();
        }

        private static SpecificationAttributeOption AddOrFindValue(SpecificationAttribute attribute, string value, int displayOrder = 0)
        {
            var val = FindValue(attribute, value);

            if (val == null)
            {
                val = new SpecificationAttributeOption()
                {
                    SpecificationAttributeId = attribute.Id,
                    Name = value,
                    DisplayOrder = displayOrder,
                };
                Context.Set<SpecificationAttributeOption>().Add(val);
                Context.SaveChanges();
            }

            return val;
        }

        private static void AddAttributeAssociation(Product product, SpecificationAttribute attribute, string value, bool allowFiltering = false, bool showOnProductPage = false)
        {
            var val = AddOrFindValue(attribute, value);
            var assoc = new ProductSpecificationAttribute()
            {
                ProductId = product.Id,
                SpecificationAttributeOptionId = val.Id,
                AllowFiltering = allowFiltering,
                ShowOnProductPage = showOnProductPage,
            };
            Context.Set<ProductSpecificationAttribute>().Add(assoc);
            Context.SaveChanges();
        }

        

        private static void AddAttributeAssociation(Product product, SpecificationAttribute ConditionAttribute, SpecificationAttributeOption specificationAttributeOption, bool allowFiltering = false, bool showOnProductPage = false)
        {
            var assoc = new ProductSpecificationAttribute()
            {
                ProductId = product.Id,
                SpecificationAttributeOptionId = specificationAttributeOption.Id,
                AllowFiltering = allowFiltering,
                ShowOnProductPage = showOnProductPage,
            };
            Context.Set<ProductSpecificationAttribute>().Add(assoc);
            Context.SaveChanges();
        }

        private static void PopulateManufacturers()
        {
            Console.Write("Fetching legacy manufacturers... ");
            var manufacturers = LegacyRepo.GameRepo.GetAllManufacturers();
            Console.WriteLine(" done!");

            int c = 0;
            foreach (var legacyMan in manufacturers)
            {
                Console.Write("{0:#,##0} Processing {1} ... ", ++c, legacyMan.strManufacturer);
                var man = Context.Set<Manufacturer>().Where(m => m.Name == legacyMan.strManufacturer).FirstOrDefault();

                if (man == null)
                {
                    Console.Write(" Creating  ");
                    man = new Manufacturer()
                    {
                        Name = legacyMan.strManufacturer,
                        PageSize = 12,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "12,18,36,72,150",
                        Published = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        UpdatedOnUtc = DateTime.UtcNow,
                    };

                    Context.Set<Manufacturer>().Add(man);
                    Context.SaveChanges();
                }
                else
                {
                    Console.Write(" Updating  ");
                }

                string s = GenerateSlug(man.Name);
                Context.Set<UrlRecord>().Add(new UrlRecord()
                {
                    EntityId = man.Id,
                    EntityName = "Manufacturer",
                    IsActive = true,
                    LanguageId = 0,
                    Slug = s,
                });
                Context.SaveChanges();
                Console.WriteLine(" slug: {0} -- done!", s);
            }
        }

        private static List<SpreadSheetRow> GetProductsToImport()
        {
            List<SpreadSheetRow> productsToImport = new List<SpreadSheetRow>();
            Console.Write("Fetching product csv list... ");
            using (StreamReader textReader = File.OpenText(@"C:\quarterarcade.com\part3.csv"))
            {
                using (var csv = new CsvReader(textReader))
                {
                    csv.Configuration.RegisterClassMap<SpreadSheetRowMap>();
                    csv.Configuration.WillThrowOnMissingField = false;
                    productsToImport.AddRange(csv.GetRecords<SpreadSheetRow>());
                }
            }
            Console.WriteLine(" done!");
            return productsToImport;
        }

        private static List<tblGames> FilterImportList(List<tblGames> allGames, List<SpreadSheetRow> productsToImport)
        {
            List<tblGames> gamesToImport = new List<tblGames>();
            Console.Write("Removing all items that aren't in the master list.. ");
            foreach (var game in allGames)
            {
                var g = productsToImport.Find(x => x.SKU == game.ID);
                if (g != null)
                    gamesToImport.Add(game);
            }
            Console.WriteLine("done!");
            return gamesToImport;
        }

        /// <summary>
        /// Makes updates to attributes that are missing.
        /// </summary>
        private static void SyncProducts()
        {
            var productsToImport = GetProductsToImport();

            Console.Write("Fetching legacy products.. ");
            var games = LegacyRepo.GameRepo.GetAllToImport2();
            Console.WriteLine(" done!");
            Console.WriteLine("{0:#,##0} products to process.", games.Count);

            var gamesToImport = FilterImportList(games, productsToImport);
            Console.WriteLine("{0:#,##0} games to import", gamesToImport.Count);

            int c = 0;
            foreach (var game in gamesToImport)
            {
                Console.Write("{0:#,##0} Checking {1} ({2}).. ", c, game.strName, game.ID);
                var product = Context.Set<Product>().Where(p => p.Sku == game.ID.ToString()).FirstOrDefault();
                if (product == null)
                {
                    Console.Write("new.. ");
                    ImportProduct(game, productsToImport.Find(x => x.SKU == game.ID));
                    Console.WriteLine("added.");
                }
                else
                {
                    Console.Write("sync.. ");
                    SyncProduct(game, product, productsToImport.Find(x => x.SKU == game.ID));
                    Console.WriteLine("done.");
                }
                c++;
            }
        }

        private static void PopulateProducts(int quantity)
        {
            var productsToImport = GetProductsToImport();

            Console.Write("Fetching legacy products... ");
            var inStockItems = LegacyRepo.GameRepo.GetAllToImport3();
            Console.WriteLine(" done!");

//            Console.WriteLine("{0:#,##0} items with manufacturer data", 
//                inStockItems.FindAll(x => !String.IsNullOrWhiteSpace(x.strManufacturer)).Count());

            var gamesToImport = FilterImportList(inStockItems, productsToImport);
            Console.WriteLine("{0:#,##0} games to import", gamesToImport.Count);

            int c = 0;
            foreach (var game in gamesToImport)
            {
                Console.Write("{2:#,##0} Processing {0} {1} ... ", game.strName, game.ID, c);

                var gameIdAttributeValue = FindValue(LegacyIdAttribute, game.ID.ToString());
                if (gameIdAttributeValue != null)
                {
                    Console.WriteLine(" ALREADY PORTED!");
                }
                else
                {
                    c++;
                    ImportProduct(game, productsToImport.Find(x => x.SKU == game.ID));
                }

                if (c > quantity) return;
            }
        }

        private static void ImportProduct(tblGames game, SpreadSheetRow row)
        {
            var product = new Product();
            UpdateProduct(game, product, row);
            Context.Set<Product>().Add(product);
            Context.SaveChanges();

            var s = GenerateSlug(product, game);

            AddAttributeAssociation(product, LegacyIdAttribute, game.ID.ToString());
            AddAttributeAssociation(product, ImageProcessedAttribute, false.ToString());

            // GGDB references. -- need to double check that RefGameName is not null..
            if (game.refRefGameEntryID > 0 && !String.IsNullOrWhiteSpace(game.RefGameName))
            {
                AddAttributeAssociation(product, LegacyReferenceGameIdAttribute, game.refRefGameEntryID.ToString());
                AddAttributeAssociation(product, LegacyReferenceGameAttribute, game.RefGameName, true, true);
            }

            // For ratings.
            if (game.nRating > 0 && game.nRating <= 10)
                AddAttributeAssociation(product, ConditionAttribute, Conditions[game.nRating], true, true);

            // Working/Non-working/Untested
            if (!String.IsNullOrWhiteSpace(game.strWorking))
            {
                if (game.strWorking == "Working")
                    game.strWorking = "Tested and Working";
                AddAttributeAssociation(product, WorkingAttribute, game.strWorking, true, true);
            }

            if (!String.IsNullOrWhiteSpace(game.strManufacturer))
            {
                var man = Context.Set<Manufacturer>().Where(m => m.Name == game.strManufacturer).FirstOrDefault();
                if (man != null)
                {
                    var pm = new ProductManufacturer()
                    {
                        ManufacturerId = man.Id,
                        ProductId = product.Id,
                    };
                    Context.Set<ProductManufacturer>().Add(pm);
                    Context.SaveChanges();
                }
            }

            // 
            if (!String.IsNullOrWhiteSpace(game.strPartType) && game.strPartType == "Board" && !String.IsNullOrWhiteSpace(game.strSubPartType))
            {
                if (game.strSubPartType != "Untested"
                    && game.strSubPartType != "Not working"
                    && game.strSubPartType != "Tested & Working")
                    AddAttributeAssociation(product, BoardTypeAttribute, game.strSubPartType, true, true);
            }

            // Console.WriteLine(" slug: {0} -- done!", s);
            Console.WriteLine(" slug: {0} -- done!", s);
        }

        private static void SyncProduct(tblGames game, Product product, SpreadSheetRow row)
        {
            UpdateProduct(game, product, row);
            Context.SaveChanges();
        }

        private static void UpdateProduct(tblGames game, Product product, SpreadSheetRow row)
        {
            // Unpublish invalid ones.
            bool publish = true;

            // None in stock
            if (game.nNumInStock <= 0)
                publish = false;
            // In a state where we don't want to publish
            else if (game.strState == ItemStatus.Private
                || game.strState == ItemStatus.PreAuction
                || game.strState == ItemStatus.eBay
                || game.strState == ItemStatus.Unavailable
                || game.strState == ItemStatus.Sold)
                publish = false;
            else
            {
                // Shopped & Available.
            }

            string description = game.strLongDescription;
            if (!String.IsNullOrWhiteSpace(game.strEBayURL))
                description = game.strLongDescription += "<p><a href='http://cgi.ebay.com/ws/eBayISAPI.dll?ViewItem&item=" + game.strEBayURL + "'>eBay Auction</a></p>";

            product.Name = game.strName;
            product.VisibleIndividually = true;
            product.ProductType = ProductType.SimpleProduct;
            product.ProductTemplateId = 1;                          // HARD CODED
            product.MetaDescription = game.strShortDescription;
            //product.MetaTitle = String.Format("{0} for sale at QuarterArcade.com", game.strName);
            product.MetaTitle = row.SeoTitle;
            product.ShortDescription = game.strShortDescription;
            product.FullDescription = description;
            product.CreatedOnUtc = game.dtCreated;
            product.UpdatedOnUtc = game.dtUpdated;
            product.Sku = game.ID.ToString();
            product.AdminComment = game.strLocation;
            product.DisplayStockQuantity = false;
            product.Price = game.mSellPrice;
            product.Weight = row.Weight;
            product.AdditionalShippingCharge = row.AdditionalShipPrice;
            product.IsShipEnabled = true;
            product.TaxCategoryId = 1;
            product.ProductCost = game.mPricePaid;
            product.ManageInventoryMethod = SmartStore.Core.Domain.Catalog.ManageInventoryMethod.ManageStock;
            product.StockQuantity = game.nNumInStock;
            product.OrderMinimumQuantity = 1;
            product.OrderMaximumQuantity = game.nNumInStock;
            product.Published = publish;
            product.MinStockQuantity = 1;
            product.LowStockActivity = LowStockActivity.Unpublish;
            product.BackorderMode = BackorderMode.NoBackorders;
        }

        private static string GenerateSlug(Product product, tblGames game)
        {
            int count = 0;
            string slug;
            while (count < 5)
            {
                if (count > 0)
                {
                    slug = GetProductSlug(game) + "-" + count.ToString();
                }
                else
                {
                    slug = GetProductSlug(game);
                }

                var s = Context
                        .Set<UrlRecord>()
                        .Where(v => v.Slug == slug)
                        .FirstOrDefault();

                if (s != null)
                {
                    count++;
                    continue;
                }

                Context.Set<UrlRecord>().Add(new UrlRecord()
                {
                    EntityId = product.Id,
                    EntityName = "Product",
                    IsActive = true,
                    LanguageId = 0,
                    Slug = slug,
                });
                Context.SaveChanges();
                return slug;
            }

            throw new ApplicationException("Could not generate a slug");
        }

        private static void BindProductToCategories()
        {
            var legacyDB = new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString);
            var allCategories = Context.Set<Category>().ToList();
            var allItems = Context.Set<Product>().ToList();

            LegacyRepo.GetAllCategories().FindAll(x => x.CatID != "All").ForEach(category =>
                {
                    // This is the category node in the new database.
                    var node = allCategories.Find(c => c.MetaKeywords == category.CatID);

                    if (node != null)
                    {
                        // Get all of the children.
                        var children = allCategories.FindAll(c => c.ParentCategoryId == node.Id);

                        // Are we at a terminal node?
                        if (children == null || children.Count == 0)
                        {
                            Console.Write("Processing terminal node: {0} {1} ", category.CatID, category.strName);

                            // builds up the sql to use.
                            string sql = String.Format((category.strParentSQL + " " + category.strSQL).Replace("{filter}", "1=1"), "20000");

                            var items = legacyDB.QuerySql<tblGames>(sql).ToList();
                            items.ForEach(item =>
                            {
                                var existingItem = allItems.Find(i => i.Sku == item.ID.ToString());
                                if (existingItem != null)
                                {
                                    var x = Context.Set<Product>().Find(existingItem.Id);
                                    x.ProductCategories.Add(new ProductCategory()
                                        {
                                            Product = x,
                                            Category = node,
                                        });
                                    Context.SaveChanges();
                                    Console.Write(". ");
                                }
                            });
                            Console.WriteLine("done!");
                        }
                    }
                });
        }

        private static void PopulateCategories()
        {

            // First let's process all of the top level categories;
            LegacyRepo
                .GetAllCategories()
                .FindAll(c => c.strParentCatID == "All.Parts" || c.CatID == "All.Parts.ReproArt")
                .ForEach(category =>
                {
                    Console.Write("Processing {0} ... ", category.strName);

                    var c = new Category()
                    {
                        Name = category.strName,
                        // DisplayOrder = category.nOrder,
                        CreatedOnUtc = new DateTime(2000, 1, 1),
                        UpdatedOnUtc = new DateTime(2000, 1, 1),
                        Published = true,
                        ShowOnHomePage = true,
                        AllowCustomersToSelectPageSize = true,
                        PageSize = 10,
                        CategoryTemplateId = 1,
                        DefaultViewMode = "grid",
                        MetaKeywords = category.CatID,          // Just to key off of.
                        MetaTitle = "Classic " + category.strName + " for sale",
                    };

                    Context.Set<Category>().Add(c);
                    Context.SaveChanges();

                    var slug = new UrlRecord()
                    {
                        EntityId = c.Id,
                        EntityName = "Category",
                        IsActive = true,
                        LanguageId = 0,
                        Slug = GetCategorySlug(category),
                    };

                    Context.Set<UrlRecord>().Add(slug);
                    Context.SaveChanges();

                    Console.WriteLine(" slug: {0} -- done!", slug.Slug);
                });

            RecursivelyPopulateChildren(Context.Set<Category>().ToList());
        }

        public static void TestCategory()
        {
            var c = new Category()
            {
                Name = "Test Category",
                // DisplayOrder = category.nOrder,
                CreatedOnUtc = new DateTime(2000, 1, 1),
                UpdatedOnUtc = new DateTime(2000, 1, 1),
                Published = true,
                ShowOnHomePage = true,
                AllowCustomersToSelectPageSize = true,
                PageSize = 10,
                CategoryTemplateId = 1,
                DefaultViewMode = "grid",
            };
            Context.Set<Category>().Add(c);
            Context.SaveChanges();
        }

        /// <summary>
        /// Returns a slug that should work.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private static string GetCategorySlug(tblCategories category)
        {
            if (category.CatID == "All")
                return "all";
            else
                return category.CatID.ToLower().Replace(".", "-").Replace("all-", "");
        }

        private static string GetProductSlug(tblGames game)
        {
            var s = GenerateSlug(String.Format("{0}-{1}", game.strName, game.ID).Trim());

            return s
                .Replace("----", "-")
                .Replace("---", "-")
                .Replace("--", "-");
        }

        private static void RecursivelyPopulateChildren(List<Category> categories)
        {
            if (categories == null || categories.Count == 0)
                return;

            categories.ForEach(category =>
                {

                    // For this category, we have to populate its children.
                    var legacyChildren = LegacyRepo
                                .GetAllCategories()
                                .FindAll(x => x.strParentCatID == category.MetaKeywords);

                    legacyChildren.ForEach(legacyChild =>
                        {
                            Console.Write("Processing {0} ... ", legacyChild.strName);

                            var newChild = new Category()
                            {
                                Name = legacyChild.strName,
                                ParentCategoryId = category.Id,
                                DisplayOrder = legacyChild.nOrder,
                                CreatedOnUtc = new DateTime(2000, 1, 1),
                                UpdatedOnUtc = new DateTime(2000, 1, 1),
                                Published = true,
                                ShowOnHomePage = false,
                                AllowCustomersToSelectPageSize = true,
                                PageSize = 10,
                                CategoryTemplateId = 1,
                                DefaultViewMode = "grid",
                                MetaKeywords = legacyChild.CatID,          // Just to key off of.
                                MetaTitle = "Classic " + legacyChild.strName + " for sale",
                            };

                            Context.Set<Category>().Add(newChild);
                            Context.SaveChanges();

                            var slug = new UrlRecord()
                            {
                                EntityId = newChild.Id,
                                EntityName = "Category",
                                IsActive = true,
                                LanguageId = 0,
                                Slug = GetCategorySlug(legacyChild),
                            };

                            Context.Set<UrlRecord>().Add(slug);
                            Context.SaveChanges();

                            Console.WriteLine(" slug: {0} -- done!", slug.Slug);

                        });

                    RecursivelyPopulateChildren(Context.Set<Category>().ToList().FindAll(y => y.ParentCategoryId == category.Id));
                });
        }

        public static string GenerateSlug(string phrase)
        {
            string str = RemoveAccent(phrase).ToLower();

            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens   
            return str;
        }

        public static string RemoveAccent(string txt)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
    }
}
