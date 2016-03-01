using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartStore.Admin.DataLoad
{
    public interface ItblRefGameEntryImagesRepository
    {
        [Sql("SELECT *, REPLACE(strImageType, 'FS:', '') AS 'ImageType'  FROM tblRefGameEntryImages WHERE strImageType LIKE 'FS:%'")]
        List<tblRefGameEntryImages> GetAllFullSize();

        [Sql("SELECT  img.* FROM [tblGameSampleImages] sample_images JOIN [tblRefGameEntryImages] img ON img.ID=sample_images.[refRefGameEntryImagesID] WHERE sample_images.refGamesId=@id")]
        List<tblRefGameEntryImages> GetSampleImagesForGame(int id);

        [Sql("SELECT * FROM tblRefGameEntryImages WHERE refRefGameEntryID=@refRefGameEntryID AND strImageType Like @name ORDER BY nOrder")]
        List<tblRefGameEntryImages> GetGenericSampleImagesForGame(int refRefGameEntryID, string name);
    }
}