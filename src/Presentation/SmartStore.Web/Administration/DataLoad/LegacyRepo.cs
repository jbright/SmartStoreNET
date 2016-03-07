using Insight.Database;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartStore.Admin.DataLoad
{
    public static class LegacyRepo
    {
        public static List<string> GetAllEMails()
        {
            return ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblReceipt2Repository>()).GetAllEmails();
        }

        public static List<tblGames> GetAllGames()
        {
            if (_allGames == null)
                _allGames = GameRepo.GetAll();
            return _allGames;
        }
        private static List<tblGames> _allGames;

        public static List<tblCategories> GetAllCategories()
        {
            if (_allCategories == null)
                _allCategories = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblCategoriesRepository>()).GetAll();
            return _allCategories;
        }
        private static List<tblCategories> _allCategories;

        public static ItblCategoriesRepository CategoryRepo
        {
            get
            {
                return ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblCategoriesRepository>()); 
                //if (_categoryRepo == null)
                //    _categoryRepo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblCategoriesRepository>());
                //return _categoryRepo;
            }
        }
        private static ItblCategoriesRepository _categoryRepo;

        public static ItblGamesRepository GameRepo
        {
            get
            {
                return ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblGamesRepository>());
                //if (_GameRepo == null)
                //    _GameRepo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblGamesRepository>());
                //return _GameRepo;
            }
        }
        private static ItblGamesRepository _GameRepo;

        public static ItblGameImagesRepository GameImageRepo
        {
            get
            {
                return ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblGameImagesRepository>());
                //if (_GameImageRepo == null)
                //    _GameImageRepo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblGameImagesRepository>());
                //return _GameImageRepo;
            }
        }
        private static ItblGameImagesRepository _GameImageRepo;

        public static ItblRefGameEntryImagesRepository ReferenceImagesRepo
        {
            get
            {
                return ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblRefGameEntryImagesRepository>());
                //if (_ReferenceImagesRepo == null)
                //    _ReferenceImagesRepo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblRefGameEntryImagesRepository>());
                //return _ReferenceImagesRepo;
            }
        }
        private static ItblRefGameEntryImagesRepository _ReferenceImagesRepo;
    }
}