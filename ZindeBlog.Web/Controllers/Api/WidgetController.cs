﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZindeBlog.Web.ViewModels.Widget;
using ZindeBlog.Web.Infrastructure.Services;

namespace ZindeBlog.Web.Controllers.Api
{
    [Area("Api")]
    [Route("api/widget")]
    [Infrastructure.Filters.RequireLoginApiFilter]
    public class WidgetController : ControllerBase
    {
        private WidgetService WidgetService { get; set; }

         public WidgetController(WidgetService widgetService)
        {
            this.WidgetService = widgetService;
        }

        [HttpGet("available")]
        public IActionResult QueryAvailable()
        {
            var widgetList = this.WidgetService.QueryAvailable();

            return this.Success(widgetList);
        }

        [HttpGet("all")]
        public async Task<IActionResult> QueryAll()
        {
            var widgetList = await this.WidgetService.Query();

            return this.Success(widgetList);
        }

        [HttpPost("")]
        public async Task<IActionResult> Save([FromBody]List<SaveWidgetModel> model)
        {
            if (model == null)
            {
                return this.InvalidRequest();
            }

            var widgetList = model.Select(t => new WidgetModel
            {
                Type = t.Type,
                Config = this.WidgetService.Transform(t.Type, t.Config)
            }).ToList();

            foreach (var widget in widgetList)
            {
                if (!widget.Config.IsValid)
                {
                    return this.InvalidRequest();
                }
            }

            var result = await this.WidgetService.Save(widgetList);

            if (result.Success)
            {
                return this.Success();
            }
            else
            {
                return this.Error(result.ErrorMessage);
            }
        }
    }
}
