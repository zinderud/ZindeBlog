﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZindeBlog.Web.ViewModels.Topic
{
    public class MonthStatisticsModel
    {
        public DateTime Month { get; set; }

        public TopicCountModel Topics { get; set; }
    }
}
