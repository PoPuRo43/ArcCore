﻿using System.Collections.Generic;
using System.Linq;

namespace ArcCore.UI.Data
{
    public class Chart
    {
        public DifficultyGroup DifficultyGroup { get; set; }
        public Difficulty Difficulty { get; set; }

        public string SongPath { get; set; }
        public string ImagePath { get; set; }

        public string Name { get; set; }
        public string NameRomanized { get; set; }
        public string Artist { get; set; }
        public string ArtistRomanized { get; set; }

        public string Bpm { get; set; }
        public float Constant { get; set; }

        public int PbScore { get; set; }
        public ScoreCategory PbGrade { get; set; }
        
        public ChartSettings Settings;

        public string Charter { get; set; }

        public string Background { get; set; }
        public Style Style { get; set; }

        public string ChartPath { get; set; }

        public IEnumerable<string> GetReferences()
        {
            yield return SongPath;
            yield return ImagePath;
            yield return ChartPath;
        }
    }
}