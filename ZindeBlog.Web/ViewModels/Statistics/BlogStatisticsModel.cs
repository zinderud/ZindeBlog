﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZindeBlog.Web.ViewModels.Statistics
{
    public class BlogStatisticsModel
    {
        public Topic.TopicCountModel Topics { get; set; }

        public Comment.CommentCountModel Comments { get; set; }

        public Page.PageCountModel Pages { get; set; }
    }
}
