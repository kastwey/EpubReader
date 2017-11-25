using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EpubReader.OutputFormaters
{
	public class HtmlOutputFormatter : StringOutputFormatter
	{
		public HtmlOutputFormatter()
		{
			SupportedMediaTypes.Add("text/html");
		}
	}
}