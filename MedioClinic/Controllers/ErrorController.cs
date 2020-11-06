﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using CMS.Base;

using XperienceAdapter.Repositories;
using Business.Configuration;
using Business.Models;

namespace MedioClinic.Controllers
{
    public class ErrorController : BaseController
    {
        private readonly IPageRepository<NamePerexText, CMS.DocumentEngine.Types.MedioClinic.NamePerexText> _pageRepository;

        private IExceptionHandlerPathFeature ExceptionHandlerPathFeature => HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        public ErrorController(
            ILogger<ErrorController> logger,
            ISiteService siteService,
            IOptionsMonitor<XperienceOptions> optionsMonitor,
            IPageRepository<NamePerexText, CMS.DocumentEngine.Types.MedioClinic.NamePerexText> pageRepository)
            : base(logger, siteService, optionsMonitor)
        {
            _pageRepository = pageRepository ?? throw new ArgumentNullException(nameof(pageRepository));
        }

        public IActionResult Index(int code)
        {
            if (code == 404)
            {
                _logger.LogError($"Not found: {ExceptionHandlerPathFeature?.Path}");

                var notFoundPage = _pageRepository.GetPages(
                    filter => filter
                        .Path("/Reused-content/Error-pages/Not-found")
                        .CombineWithDefaultCulture(),
                    buildCacheAction: cache => cache
                        .Key($"{nameof(ErrorController)}|NotFoundPage")
                        .Dependencies((_, builder) => builder
                            .PageType(CMS.DocumentEngine.Types.MedioClinic.NamePerexText.CLASS_NAME)),
                    includeAttachments: true)
                        .FirstOrDefault();

                var viewModel = GetPageViewModel(notFoundPage, notFoundPage?.Name!);


                return View("NotFound", viewModel);
            }

            _logger.LogError(ExceptionHandlerPathFeature?.Error, string.Empty);

            return StatusCode(code);
        }
    }
}
