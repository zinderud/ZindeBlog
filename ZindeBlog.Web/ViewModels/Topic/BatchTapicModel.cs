﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ZindeBlog.Web.ViewModels.Tag
{
    public class BatchTopicModel
    {
        [Required]
        public int[] TopicList { get; set; }
    }
}
