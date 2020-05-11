﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using XperienceAdapter;
using MedioClinic.Models;
using Business;
using MedioClinic.Configuration;

namespace MedioClinic.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IOptionsService<XperienceOptions> optionsService)
        {
            _logger = logger;
            _ = optionsService;
        }

        public IActionResult Index()
        {
            var dbConnectivityTest = new DbConnectivityTest();
            var documentGuid = dbConnectivityTest.GetDocumentGuid();
            ViewBag.DocumentGuid = documentGuid;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
