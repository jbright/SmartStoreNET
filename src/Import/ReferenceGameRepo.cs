using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public interface ReferenceGameRepo
    {
        [Sql(@"
            select 
	            game.Id As 'Id',
	            game.strName as 'Name',
	            game.strGenre as 'Genre',
	            game.strDescription as 'Description',
	            ent.ID AS 'EntryId', 
	            ent.nYear as 'Year',
	            ent.strManufacturer as 'Manufacturer',
	            ent.strAdditionalDescription as 'AdditionalDescription',
	            ent.strMonitorSync as 'MonitorSync', 
	            ent.strMonitorComposite as 'MonitorComposite',
	            ent.strMonitorOrientation as 'MonitorOrientation',
	            ent.strMonitorResolution as 'MonitorResolution',
	            ent.strMonitorType as 'MonitorType',
	            ent.strConversionType as 'ConversionType',
	            ent.strNumberPlayers as 'NumberPlayers'
            from 
	            tblRefGame game
	            JOIN tblRefGameEntry ent ON ent.refRefGameId = game.Id
            WHERE
	            strClass='Coin-Op'
	            AND strSubClass='Arcade'
	            AND bValidated=1
	            AND bDisabled=0
            ORDER BY ID, EntryId  
        ")]
        List<ReferenceGame> GetAllValid();

        [Sql("SELECT * FROM QA_ReferenceGame WHERE Id=@id")]
        ReferenceGame GetById(int id);

        [Sql("SELECT * FROM QA_ReferenceGame WHERE Name=@name")]
        ReferenceGame GetByName(string name);

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

        [Sql(@"

        select 
	        img.*, game.strName  
        from 
	        tblrefGameEntryImages img
	        JOIN tblRefGameEntry ent ON ent.ID=img.refRefGameEntryId
	        JOIN tblRefGame game ON game.Id = ent.refRefGameId
        WHERE
	        strClass='Coin-Op'
	        AND strSubClass='Arcade'
	        AND bValidated=1
	        AND bDisabled=0
	        and img.strImageType = 'FS:Screen'
        ORDER BY refRefGameEntryId, nOrder 
        ")]
        List<tblRefGameEntryImage> GetAllValidScreenCaptures();


        [Sql(@"INSERT INTO QA_ReferenceGamePicture (ReferenceGameId, Path, Description, Width, Height) VALUES (@ReferenceGameId, @Path, @Description, @Width, @Height)")]
        void AddReferencePicture(int ReferenceGameId, string Path, string Description, int Width, int Height);
    }
}
