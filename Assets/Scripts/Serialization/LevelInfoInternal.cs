﻿namespace ArcCore.Serialization
{
    public class LevelInfoInternal
    {
        public ChartInfo[] charts;
        public string[] importedGlobals;

        public LevelInfoInternal(LevelInfo levelInfo, string[] importedGlobals)
        {
            charts = levelInfo.charts;

            for (int i = 0; i < charts.Length; i++)
            {
                charts[i].styleOverride = charts[i].styleOverride ?? levelInfo.style;
                charts[i].songInfoOverride = charts[i].songInfoOverride ?? levelInfo.songInfo;
            }

            this.importedGlobals = importedGlobals;
        }
    }
}