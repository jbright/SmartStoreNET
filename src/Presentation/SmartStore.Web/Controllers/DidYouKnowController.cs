using Insight.Database;
using SmartStore.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmartStore.Web.Controllers
{
    public class DidYouKnowController : PublicControllerBase
    {
        [ChildActionOnly()]
        public ActionResult Random()
        {
            return View(GetRandomQuote());
        }

        private DidYouKnow GetRandomQuote()
        {
            var quotes = GetQuotes();
            return quotes[RandomNumberGenerator.Next(0, quotes.Count)];
        }

        private List<DidYouKnow> GetQuotes()
        {
            if (_quotes == null)
            {
                lock (_lock)
                {
                    if (_quotes == null)
                    {
                        _quotes = ((new SqlConnection(ConfigurationManager.ConnectionStrings["EC"].ConnectionString))
                            .As<DidYouKnowRepository>())
                            .GetAll();
                    }
                }
            }
            return _quotes;
        }
        private static List<DidYouKnow> _quotes;
        private static object _lock = new object();

        private static Random RandomNumberGenerator = new Random();
    }

    public class DidYouKnow
    {
        int Id { get; set; }
        public string Text { get; set; }
    }

    public interface DidYouKnowRepository
    {
        [Sql("SELECT * FROM QA_DidYouKnow")]
        List<DidYouKnow> GetAll();
    }
}