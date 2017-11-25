﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EpubReader.Models
{
    public class Chapter
    {
		public book Book { get; set; }

		public string Title { get; set; }

		public string FileName { get; set; }

		public List<Chapter> Chapters { get; set; } = new List<Chapter>();

	}
}
