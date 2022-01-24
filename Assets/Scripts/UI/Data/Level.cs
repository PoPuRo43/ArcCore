using ArcCore.Utitlities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityCoroutineUtils;

namespace ArcCore.UI.Data
{
    public class Level : IArccoreInfo
    {
        public ulong Id { get; set; }

        public string Directory { get; set; }
        public IList<string> ImportedGlobals { get; set; }

        public Chart[] Charts { get; set; }
        public Pack Pack { get; set; }

        public Chart GetClosestChart(DifficultyGroup difficultyGroup)
        {
            Chart result = null;
            float closestDifference = float.PositiveInfinity;

            foreach (Chart chart in Charts)
            {
                float diff = Math.Abs(chart.DifficultyGroup.Precedence - difficultyGroup.Precedence);
                if (diff < closestDifference)
                {
                    result = chart;
                    closestDifference = diff;
                }
            }

            return result;
        }

        public Chart GetExactChart(DifficultyGroup difficultyGroup)
        {
            foreach (Chart chart in Charts)
            {
                if (chart.DifficultyGroup.Precedence == difficultyGroup.Precedence)
                    return chart;
            }
            return null;
        }

        public void ModifyReferences(Func<string, string> modifier)
        {
            foreach(var c in Charts)
            {
                c.SongPath = modifier(c.SongPath);
                c.ImagePath = modifier(c.ImagePath);
                c.ChartPath = modifier(c.ChartPath);
            }
        }

        public IEnumerable<string> References 
            => Charts.SelectMultiple(c => c.ImagePath, c => c.SongPath, c => c.ChartPath);
    }
}