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
using SmartStore.Admin.Models.ReferenceGames;

namespace SmartStore.Admin.Controllers
{
    [AdminAuthorize]
    public class ReferenceGamesController : AdminControllerBase
    {
        #region Constructors

        public ReferenceGamesController()
        {
        }
        
        #endregion

        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List(ReferenceGamesListModel model)
        {
            return View(model);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult GamesList(GridCommand command, ReferenceGamesListModel model)
        {
            var gridModel = new GridModel<ReferenceGame>();
            var games = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                            .As<ReferenceGameRepository>())
                            .GetAll();
            if (!String.IsNullOrWhiteSpace(model.Name))
            {
                string lowerAuthor = model.Name.ToLower();
                games = games.FindAll(g => g.Name.ToLower().Contains(lowerAuthor));
            }

            gridModel.Data = games.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize);
            gridModel.Total = games.Count();

            return new JsonResult
            {
                Data = gridModel
            };
        }

        public ActionResult Create()
        {
            var model = new ReferenceGame();
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Create(ReferenceGame model, bool continueEditing)
        {
            ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                                        .As<ReferenceGameRepository>())
                                        .Add(model);

            NotifySuccess("Reference Game added.");
            return RedirectToAction("List");
        }

        public ActionResult Edit(int id)
        {
            var model = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                                        .As<ReferenceGameRepository>())
                                        .GetById(id);
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Edit(ReferenceGame model, bool continueEditing)
        {
            ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                                        .As<ReferenceGameRepository>())
                                        .Update(model);

            return RedirectToAction("List");
        }

    }

    public interface ReferenceGameRepository
    {
        [Sql("SELECT * FROM QA_ReferenceGame WHERE Name=@name")]
        ReferenceGame GetByName(string name);

        [Sql("SELECT * FROM QA_ReferenceGame WHERE Id=@id")]
        ReferenceGame GetById(int id);

        [Sql("SELECT * FROM QA_ReferenceGame ORDER BY Name")]
        List<ReferenceGame> GetAll();

        [Sql(@"
            INSERT INTO 
                QA_ReferenceGame 
                (Id, Name, Genre, Description, EntryId, Year, Manufacturer, AdditionalDescription,
                MonitorSync, MonitorComposite, MonitorOrientation, MonitorResolution, MonitorType,
                ConversionType, NumberPlayers) 
            VALUES 
                (@Id, @Name, @Genre, @Description, @EntryId, @Year, @Manufacturer, @AdditionalDescription,
                @MonitorSync, @MonitorComposite, @MonitorOrientation, @MonitorResolution, @MonitorType,
                @ConversionType, @NumberPlayers)")]
        ReferenceGame Add(ReferenceGame game);

        [Sql(@"
            UPDATE 
                QA_ReferenceGame
            SET
                Name = @Name, 
                Genre = @Genre, 
                Description = @Description, 
                EntryId = @EntryId, 
                Year = @Year, 
                Manufacturer = @Manufacturer, 
                AdditionalDescription = @AdditionalDescription,
                MonitorSync = @MonitorSync, 
                MonitorComposite = @MonitorComposite, 
                MonitorOrientation = @MonitorOrientation, 
                MonitorResolution = @MonitorResolution, 
                MonitorType = @MonitorType,
                ConversionType = @ConversionType, 
                NumberPlayers = @NumberPlayers
            WHERE
                Id = @id
            ")]
        ReferenceGame Update(ReferenceGame game);
    }
}