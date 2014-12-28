using BoboBrowse.Net;
using CarDemo.BoboServices;
using CarDemo.Models;
using System;
using System.Web.Mvc;

namespace CarDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly BrowseRequestConverter browseRequestConverter;
        private readonly BrowseService browseService;

        public HomeController()
        {
            browseRequestConverter = new BrowseRequestConverter();
            browseService = new BrowseService();
        }

        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        //
        // POST: /Home/Browse/

        [HttpPost]
        public ActionResult Browse(BoboRequest boboRequest)
        {
            BrowseRequest browseRequest = this.browseRequestConverter.ConvertBrowseRequest(boboRequest);

            using (var browseResult = this.browseService.Browse(browseRequest))
            {
                return Json(new BoboResult(browseResult), "application/json");
            }
        }
    }
}
