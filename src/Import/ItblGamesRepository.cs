using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public interface ItblGamesRepository
    {
        [Sql("SELECT * FROM tblGames ORDER BY ID")]
        List<tblGames> GetAll();

        [Sql(@"SELECT g.*, e.strClass, e.strSubClass, e.nYear, e.strManufacturer, rg.strName AS 'RefGameName' 
            FROM tblGames g
	            LEFT JOIN tblRefGameEntry e ON e.ID=g.refRefGameEntryId
	            LEFT JOIN tblRefGame rg ON rg.ID=e.refRefGameID
            WHERE
            g.strState = 'eBay'
            OR g.strState = 'PreAuction'
            OR g.strState = 'Shopped'
            OR g.strState = 'Private'
            OR g.strState = 'PreAuction'
            ")]
        List<tblGames> GetAllToImport();

        [Sql(@"SELECT g.*, e.strClass, e.strSubClass, e.nYear, e.strManufacturer, rg.strName AS 'RefGameName' 
            FROM tblGames g
	            LEFT JOIN tblRefGameEntry e ON e.ID=g.refRefGameEntryId
	            LEFT JOIN tblRefGame rg ON rg.ID=e.refRefGameID
            WHERE dtUpdated > '2010-01-01' OR 
	            ((g.strState = 'eBay'
	            OR g.strState = 'PreAuction'
	            OR g.strState = 'Shopped' 
	            OR g.strState = 'Private'
	            OR g.strState = 'Shopped'
	            OR g.strState = 'Available')
	            AND nNumInStock > 0)
            ")]
        List<tblGames> GetAllToImport2();

        [Sql(@"SELECT g.*, e.strClass, e.strSubClass, e.nYear, e.strManufacturer, rg.strName AS 'RefGameName' 
            FROM tblGames g
	            LEFT JOIN tblRefGameEntry e ON e.ID=g.refRefGameEntryId
	            LEFT JOIN tblRefGame rg ON rg.ID=e.refRefGameID
            WHERE dtUpdated > '2010-01-01'
            ")]
        List<tblGames> GetAllToImport3();

        [Sql(@"SELECT DISTINCT e.strManufacturer
            FROM tblGames g
	            LEFT JOIN tblRefGameEntry e ON e.ID=g.refRefGameEntryId
	            LEFT JOIN tblRefGame rg ON rg.ID=e.refRefGameID
            WHERE
            (g.strState = 'eBay'
            OR g.strState = 'PreAuction'
            OR g.strState = 'Shopped'
            OR g.strState = 'Private'
            OR g.strState = 'PreAuction')
            AND  e.strManufacturer IS NOT NULL
            ORDER BY e.strManufacturer")]
        List<tblGames> GetAllManufacturers();
    }
}
