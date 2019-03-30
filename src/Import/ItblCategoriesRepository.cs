﻿using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Import
{
    public interface ItblCategoriesRepository
    {
        [Sql("SELECT * FROM tblCategories2 ORDER BY CatID")]
        List<tblCategories> GetAll();

        [Sql("SELECT * FROM tblCategories2 ORDER BY LEN(strParentArray) DESC")]
        List<tblCategories> GetAllDeepFirst();
    }
}