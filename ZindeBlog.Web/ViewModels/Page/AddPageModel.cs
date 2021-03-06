﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ZindeBlog.Web.ViewModels.Page
{
    public class AddPageModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(100)]
        public string Alias { get; set; }

        [StringLength(100)]
        public string Keywords { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime? Date { get; set; }

        public int? Parent { get; set; }

        public int Order { get; set; }

        public bool IsHomePage { get; set; }

        public bool ShowInList { get; set; }

        public Infrastructure.Enums.PageStatus Status { get; set; }
    }
}
