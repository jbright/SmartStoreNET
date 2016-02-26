using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartStore.Admin.DataLoad
{
    public interface ItblGameImagesRepository
    {
        [Sql("SELECT * FROM [tblGameImages2] WHERE strImageType='Fullsize' OR strImageType='Profile' ORDER BY refgamesid, nOrder")]
        List<tblGameImages> GetAll();

        [Sql("SELECT * FROM [tblGameImages2] WHERE refGamesID=@id AND (strImageType='Fullsize') ORDER BY refgamesid, nOrder")]
        List<tblGameImages> GetAllForGame(int Id);

        [Sql("select distinct refgamesid from [tblGameImages2] order by refgamesid")]
        List<int> GetDistinctGames();
    }
}