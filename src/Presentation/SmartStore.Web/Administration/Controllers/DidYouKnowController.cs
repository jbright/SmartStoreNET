using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SmartStore.Admin.Models.Directory;
using SmartStore.Core;
using SmartStore.Core.Domain.Directory;
using SmartStore.Services.Configuration;
using SmartStore.Services.Directory;
using SmartStore.Services.Helpers;
using SmartStore.Services.Localization;
using SmartStore.Services.Security;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Filters;
using SmartStore.Web.Framework.Security;
using Telerik.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;

namespace SmartStore.Admin.Controllers
{
    [AdminAuthorize]
    public class DidYouKnowController : AdminControllerBase
    {
        #region Constructors

        public DidYouKnowController()
        {
        }
        
        #endregion

        // GET: DidYouKnow
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            var quotes = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                            .As<DidYouKnowRepository>())
                            .GetAll();

            var gridModel = new GridModel<DidYouKnow>
            {
                Data = quotes,
                Total = quotes.Count()
            };
            return View(gridModel);
        }

        public ActionResult Create()
        {
            var model = new DidYouKnow();
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Create(DidYouKnow model, bool continueEditing)
        {
            ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                                        .As<DidYouKnowRepository>())
                                        .Add(model.Text);

            NotifySuccess("Did you know added.");
            return RedirectToAction("List");
        }

        public ActionResult Edit(int id)
        {
            var model = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                                        .As<DidYouKnowRepository>())
                                        .GetById(id);
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Edit(DidYouKnow model, bool continueEditing)
        {
            ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                                        .As<DidYouKnowRepository>())
                                        .Update(model.Id, model.Text);

            return RedirectToAction("List");
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                                        .As<DidYouKnowRepository>())
                                        .Delete(id);

            return RedirectToAction("List");
        }
    }

    public class DidYouKnow
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        public string Text { get; set; }
    }

    public interface DidYouKnowRepository
    {
        [Sql("SELECT * FROM QA_DidYouKnow")]
        List<DidYouKnow> GetAll();

        [Sql("INSERT INTO QA_DidYouKnow ([Text]) VALUES (@quote)")]
        void Add(string quote);

        [Sql("UPDATE QA_DidYouKnow SET [Text]=@quote WHERE Id=@id")]
        void Update(int id, string quote);

        [Sql("SELECT * FROM QA_DidYouKnow WHERE Id = @id")]
        DidYouKnow GetById(int id);

        [Sql("DELETE FROM QA_DidYouKnow WHERE Id = @id")]
        void Delete(int id);
    }
}