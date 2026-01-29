using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using QRiskTree.Engine;
using QRiskTreeEditor.ViewModels;
using System.Web;
using System.Windows.Controls;

namespace QRiskTreeEditor.Controls
{
    public partial class RangeDistributionChart : UserControl
    {
        private const int DefaultBucketCount = 500;
        private int _bucketCount = DefaultBucketCount;
        private double _bucketSize = 0;

        public RangeDistributionChart()
        {
            InitializeComponent();
        }

        #region Plotting Methods.
        internal void Plot(double[] data, int minPercentile, int maxPercentile, 
            string? currencySymbol = null, string? monetaryScale = null)
        {
            if (data == null || data.Length == 0)
                return;

            double min = data.Min();
            double max = data.Max();
            _bucketCount = Math.Min(DefaultBucketCount, data.Length / 100);
            double bucketSize = (max - min) / _bucketCount;

            // Histogram
            var histogramSeries = new HistogramSeries
            {
                Title = "Histogram",
                FillColor = OxyColors.SkyBlue
            };

            // Percentile line (unchanged)
            var percentileSeries = new LineSeries
            {
                Title = "Percentile",
                Color = OxyColors.Red,
                YAxisKey = "PercentileAxis"
            };

            var partial = 0;

            for (int i = 0; i < _bucketCount; i++)
            {
                double bucketStart = min + i * bucketSize;
                double bucketEnd = bucketStart + bucketSize;
                int count = data.Count(v => v >= bucketStart && v < bucketEnd);
                if (i == _bucketCount - 1)
                    count += data.Count(v => v == max);
                partial += count;

                double area = count * bucketSize;
                histogramSeries.Items.Add(new HistogramItem(bucketStart, bucketEnd, area, count));
                percentileSeries.Points.Add(new DataPoint(bucketStart + (bucketSize / 2), ((double) partial) / ((double) data.Length) * 100));
            }

            // Plot model
            var model = new PlotModel();

            var valueSuffix = currencySymbol != null ? " ({monetaryScale}{currencySymbol})" : string.Empty;

            // X Axis
            model.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Minimum = min,
                    Maximum = max,
                    Title = $"Value{valueSuffix}",
                    StringFormat = "N0",
                    MajorStep = GetNiceStep(min, max)
                });

            // Histogram Y Axis
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Count",
                StringFormat = "N0"
            });

            // Percentile Y Axis
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Right,
                Key = "PercentileAxis",
                Minimum = 0,
                Maximum = 100,
                Title = "Percentile (%)"
            });

            model.Series.Add(histogramSeries);
            model.Series.Add(percentileSeries);

            // Horizontal lines for percentiles
            var lineMinPercentile = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = minPercentile,
                Color = OxyColors.Green,
                LineStyle = LineStyle.Dash,
                Text = $"{minPercentile}th Percentile",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                YAxisKey = "PercentileAxis"
            };
            var lineMaxPercentile = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = maxPercentile,
                Color = OxyColors.Orange,
                LineStyle = LineStyle.Dash,
                Text = $"{maxPercentile}th Percentile",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                YAxisKey = "PercentileAxis"
            };
            model.Annotations.Add(lineMinPercentile);
            model.Annotations.Add(lineMaxPercentile);

            // Vertical lines for percentile values
            var range = data.ToRange(RangeType.Number, minPercentile, maxPercentile);
            if (range != null)
            {
                var average = data.Average();

                var formattedMin = currencySymbol != null ? range.GetMin(currencySymbol, monetaryScale) : range.Min.ToString("F2");
                var formattedMode = currencySymbol != null ? range.GetMode(currencySymbol, monetaryScale) : range.Mode.ToString("F2");
                var formattedMax = currencySymbol != null ? range.GetMax(currencySymbol, monetaryScale) : range.Max.ToString("F2");
                var formattedAverage = currencySymbol != null ? average.FormatMoney(currencySymbol, monetaryScale) : average.ToString("F2");

                var lineMinValue = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = range.Min,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"{minPercentile}th Percentile: {formattedMin}",
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top
                };

                var lineModeValue = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = range.Mode,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"Mode: {formattedMode}",
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top,
                    TextMargin = 250
                };

                var lineAverageValue = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = average,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"Average: {formattedAverage}",
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top
                };

                var lineMaxValue = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = range.Max,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"{maxPercentile}th Percentile: {formattedMax}",
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top
                };

                model.Annotations.Add(lineMinValue);
                model.Annotations.Add(lineModeValue);
                model.Annotations.Add(lineAverageValue);
                model.Annotations.Add(lineMaxValue);
            }

            PlotView.Model = model;
        }

        private static double GetNiceStep(double min, double max, int maxSteps = 10)
        {
            double range = max - min;
            if (range <= 0) return 1;
            double roughStep = range / maxSteps;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
            double[] niceSteps = { 1, 2, 5, 10 };
            double bestStep = niceSteps.Select(f => f * magnitude).OrderBy(f => Math.Abs(f - roughStep)).First();
            return bestStep;
        }
        #endregion
    }
}