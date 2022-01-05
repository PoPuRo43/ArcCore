﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.UI.Data
{
    public class Level : IArccoreInfo
    {
        public string Directory { get; set; }
        public IList<string> ImportedGlobals { get; set; }

        public Chart[] Charts { get; set; }

        public IEnumerable<string> GetReferences()
            => Charts.SelectMany(c => c.GetReferences());
    }
}