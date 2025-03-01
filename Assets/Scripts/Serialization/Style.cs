﻿using Newtonsoft.Json;
using System;

namespace ArcCore.Serialization
{
    public class Style
    {
        public StyleScheme scheme;


        [JsonIgnore]
        private string _background;
        [JsonProperty(PropertyName = "bg")]
        public string Background
        {
            get => _background;
            set
            {
                if (value.StartsWith("$"))
                {
                    if (!FileStatics.IsValidGlobalName(value.Substring(1)))
                        throw new Exception("Invalid global item name");
                }

                _background = value;
            }
        }
    }
}