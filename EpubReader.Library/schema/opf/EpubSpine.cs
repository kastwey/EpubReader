﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubReader.Library.Schema.Opf
{
    public class EpubSpine : List<EpubSpineItemRef>
    {
        public string Toc { get; set; }
    }
}
