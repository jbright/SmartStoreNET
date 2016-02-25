using Insight.Database;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public static class LegacyRepo
    {
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
                if (_categoryRepo == null)
                    _categoryRepo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblCategoriesRepository>());
                return _categoryRepo;
            }
        }
        private static ItblCategoriesRepository _categoryRepo;

        public static ItblGamesRepository GameRepo
        {
            get
            {
                if (_GameRepo == null)
                    _GameRepo = ((new SqlConnection(ConfigurationManager.ConnectionStrings["LegacyDB"].ConnectionString)).As<ItblGamesRepository>());
                return _GameRepo;
            }
        }
        private static ItblGamesRepository _GameRepo;


    }

}
