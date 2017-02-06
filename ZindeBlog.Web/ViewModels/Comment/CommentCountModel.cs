﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZindeBlog.Web.ViewModels.Comment
{
    public class CommentCountModel
    {
        public int Pending { get; set; }

        public int Approved { get; set; }

        public int Reject { get; set; }

        public int Total { get; set; }
    }
}
