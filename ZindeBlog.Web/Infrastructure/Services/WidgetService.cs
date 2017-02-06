﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZindeBlog.Web.Entities.Blog;
using ZindeBlog.Web.Infrastructure.Core;
using ZindeBlog.Web.Infrastructure.Enums;
using ZindeBlog.Web.Infrastructure.Extensions;
using ZindeBlog.Web.ViewModels.Widget;

namespace ZindeBlog.Web.Infrastructure.Services
{
    public class WidgetService
    {
        private static readonly string CacheKey = "Cache_Widget";

        private static readonly Dictionary<WidgetType, string> DefaultNames = new Dictionary<WidgetType, string>
        {
            { WidgetType.Administration, "Administration" },
            { WidgetType.Category, "Category" },
            { WidgetType.RecentComment, "RecentComment" },
            { WidgetType.MonthStatistics, "MonthStatistics" },
            { WidgetType.Page, "Page" },
            { WidgetType.Search, "Search" },
            { WidgetType.Tag, "Tag" },
            { WidgetType.RecentTopic, "RecentTopic" },
            { WidgetType.Link, "Link" }
        };

        private static readonly Dictionary<WidgetType, Type> DefaultWidgetConfigTypes = new Dictionary<WidgetType, Type>
        {
            { WidgetType.Administration, typeof(AdministrationWidgetConfigModel) },
            { WidgetType.Category, typeof(CategoryWidgetConfigModel) },
            { WidgetType.RecentComment, typeof(RecentCommentWidgetConfigModel) },
            { WidgetType.MonthStatistics, typeof(MonthStatisticeWidgetConfigModel) },
            { WidgetType.Page, typeof(PageWidgetConfigModel) },
            { WidgetType.Search, typeof(SearchWidgetConfigModel) },
            { WidgetType.Tag, typeof(TagWidgetConfigModel) },
            { WidgetType.RecentTopic, typeof(RecentTopicWidgetConfigModel) },
            { WidgetType.Link, typeof(LinkWidgetConfigModel) }
        };

        private ZindeBlogContext BlogContext { get; set; }

        private IMemoryCache Cache { get; set; }

        public WidgetService(ZindeBlogContext blogContext, IMemoryCache cache)
        {
            this.BlogContext = blogContext;
            this.Cache = cache;
        }

        public List<AvailableWidgetModel> QueryAvailable()
        {
            List<AvailableWidgetModel> result = new List<AvailableWidgetModel>();

            var arr = Enum.GetValues(typeof(WidgetType));
            foreach (byte item in arr)
            {
                WidgetType type = (WidgetType)item;
                Type configType = DefaultWidgetConfigTypes[type];
                result.Add(new AvailableWidgetModel
                {
                    Type = type,
                    Name = DefaultNames[type],
                    DefaultConfig = (WidgetConfigModelBase)Activator.CreateInstance(configType),
                    Icon = type.ToString()
                });
            }

            result = result.OrderBy(t => t.Type).ToList();

            return result;
        }

        private async Task<List<Widget>> All()
        {
            var result = await this.Cache.RetriveCacheAsync(CacheKey, async () =>
            {
                var list = await this.BlogContext.Widgets.ToListAsync();
                return list;
            });

            return result;
        }

        public async Task<List<WidgetModel>> Query()
        {
            var entityList = await this.All();

            var result = entityList.OrderBy(t => t.ID).Select(t => new WidgetModel
            {
                Type = t.Type,
                Config = JsonConvert.DeserializeObject(t.Config, DefaultWidgetConfigTypes[t.Type]) as WidgetConfigModelBase
            });

            return result.ToList();
        }

        public async Task<OperationResult> Save(List<WidgetModel> widgetList)
        {
            using (var tran = await this.BlogContext.Database.BeginTransactionAsync())
            {
                var entityList = await this.BlogContext.Widgets.ToListAsync();
                this.BlogContext.RemoveRange(entityList);
                await this.BlogContext.SaveChangesAsync();

                entityList = widgetList.Select(t => new Widget
                {
                    Type = t.Type,
                    ID = widgetList.IndexOf(t) + 1,
                    Config = JsonConvert.SerializeObject(t.Config)
                }).ToList();
                this.BlogContext.AddRange(entityList);
                await this.BlogContext.SaveChangesAsync();

                this.Cache.Remove(CacheKey);

                tran.Commit();
                return new OperationResult();
            }
        }

        public WidgetConfigModelBase Transform(WidgetType type, JObject config)
        {
            Type targetType = DefaultWidgetConfigTypes[type];
            return config.ToObject(targetType) as WidgetConfigModelBase;
        }
    }
}
