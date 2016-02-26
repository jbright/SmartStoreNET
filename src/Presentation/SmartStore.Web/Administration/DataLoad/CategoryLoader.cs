using SmartStore.Core.Domain.Catalog;
using SmartStore.Services.Catalog;
using SmartStore.Services.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SmartStore.Admin.DataLoad
{
    public class CategoryLoader
    {
        private ICategoryService CategoryService { get; set; }

        private IUrlRecordService UrlRecordService { get; set; }

        public string Progress
        {
            get
            {
                return sb.ToString();
            }
        }
        private StringBuilder sb;

        public CategoryLoader(ICategoryService categoryService, IUrlRecordService urlRecordService)
        {
            CategoryService = categoryService;
            UrlRecordService = urlRecordService;
        }

        public void Start()
        {
            sb = new StringBuilder();

            // First let's process all of the top level categories;
            LegacyRepo
                .GetAllCategories()
                .FindAll(c => c.strParentCatID == "All.Parts" || c.CatID == "All.Parts.ReproArt")
                .ForEach(category =>
                {
                    sb.AppendFormat("Processing {0} ... ", category.strName);

                    var c = new Category()
                    {
                        Name = category.strName,
                        DisplayOrder = category.nOrder,
                        CreatedOnUtc = new DateTime(2000, 1, 1),
                        UpdatedOnUtc = new DateTime(2000, 1, 1),
                        Published = true,
                        ShowOnHomePage = true,
                        AllowCustomersToSelectPageSize = true,
                        PageSize = 10,
                        CategoryTemplateId = 1,
                        DefaultViewMode = "grid",
                        MetaKeywords = category.CatID,          // Just to key off of.
                    };

                    CategoryService.InsertCategory(c);
                    sb.AppendFormat(" Entity ID {0} ", c.Id);

                    var slug = c.ValidateSeName(null, c.Name, true);
                    UrlRecordService.SaveSlug(c, slug, 0);
                    sb.AppendLine("done!");
                });

            RecursivelyPopulateChildren(CategoryService.GetAllCategoriesByParentCategoryId(0).ToList());

        }
        private void RecursivelyPopulateChildren(List<Category> categories)
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
                    sb.AppendFormat("Processing {0} ... ", legacyChild.strName);

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
                    };

                    CategoryService.InsertCategory(newChild);
                    sb.AppendFormat(" Entity ID {0} ", newChild.Id);

                    var slug = category.ValidateSeName(null, category.Name, true);
                    UrlRecordService.SaveSlug(newChild, slug, 0);
                    sb.AppendLine("done!");
                });

                RecursivelyPopulateChildren(CategoryService.GetAllCategoriesByParentCategoryId(category.Id).ToList());
            });
        }

    }
}