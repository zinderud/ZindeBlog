﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZindeBlog.Web.ViewModels.Topic
{
    public interface ITopicModel
    {
        int ID { get; }

        string Title { get; }

        string Alias { get; }
    }
}
