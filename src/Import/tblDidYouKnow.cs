using Insight.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    public class tblDidYouKnow
    {
        public int ID { get; set; }

        public string strDYK { get; set; }
    }

    public interface ItblDidYouKnow
    {
        [Sql("select * from tblDidYouKnow")]
        List<tblDidYouKnow> GetAll();

        [Sql("INSERT INTO QA_DidYouKnow (Text) VALUES (@quote)")]
        void Add(string quote);
    }

}
