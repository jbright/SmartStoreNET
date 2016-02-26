using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartStore.Admin.DataLoad
{
    public interface ItblGamesRepository
    {
        [Sql("SELECT * FROM tblGames ORDER BY ID")]
        List<tblGames> GetAll();

        [Sql("SELECT * FROM tblGames WHERE ID=@id")]
        tblGames GetById(int id);
    }
}