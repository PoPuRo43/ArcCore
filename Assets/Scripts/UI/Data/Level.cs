namespace ArcCore.UI.Data
{
    public class Level
    {
        public Chart[] Charts { get; set; }
        public Pack pack { get; set; }

        public Chart GetClosestChart(Difficulty difficulty)
        {
            Chart result = null;
            float closestDifference = float.PositiveInfinity;

            foreach (Chart chart in Charts)
            {
                if (Math.Abs(chart.Difficulty.Precedence - difficulty.Precedence) < closestDifference)
                    result = chart;
            }

            return result;
        }

        public Chart GetExactChart(Difficulty difficulty)
        {
            foreach (Chart chart in Charts)
            {
                if (chart.Difficulty.Precedence == difficulty.Precedence)
                    return chart;
            }
            return null;
        }
    }
}