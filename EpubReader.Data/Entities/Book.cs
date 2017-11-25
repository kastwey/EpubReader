using System;
using System.Collections.Generic;
using System.Text;

namespace EpubReader.Data.Entities
{
	public class Book
	{



		public string Title { get; set; }

		public string Author { get; set; }

		public string Path { get; set; }

		public DateTime? LastOpened { get; set; }

		public ushort Stars { get; set; }

	}
}
