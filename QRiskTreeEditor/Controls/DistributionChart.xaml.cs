using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using QRiskTreeEditor.ViewModels;
using System.Windows.Controls;

namespace QRiskTreeEditor.Controls
{
    public partial class DistributionChart : UserControl
    {
        private RiskModelViewModel? _model;

        public DistributionChart()
        {
            InitializeComponent();
        }

        internal void SetModel(RiskModelViewModel model, RelevantEvent relevantEvent)
        {
            if (_model != null)
            {
                _model.Model.BaselineSimulationCompleted -= BaselineSimulationCompleted;
                _model.Model.OptimalFirstYearSimulationCompleted -= OptimizationCompleted;
                _model.Model.OptimalFollowingYearsSimulationCompleted -= OptimizationCompleted;
                PlotView.Model = new PlotModel();
            }

            _model = model;

            switch (relevantEvent)
            {
                case RelevantEvent.Baseline:
                    model.Model.BaselineSimulationCompleted += BaselineSimulationCompleted;
                    break;
                case RelevantEvent.FirstYear:
                    model.Model.OptimalFirstYearSimulationCompleted += OptimizationCompleted;
                    break;
                case RelevantEvent.FollowingYears:
                    model.Model.OptimalFollowingYearsSimulationCompleted += OptimizationCompleted;
                    break;
            }
        }

        private void BaselineSimulationCompleted(double[] samples)
        {
            Plot(samples);
        }

        private void OptimizationCompleted(IEnumerable<Guid>? mitigationIds, double[] samples)
        {
            Plot(samples);
        }

        public void Plot(double[] data)
        {
            if (data == null || data.Length == 0)
                return;

            double min = data.Min();
            double max = data.Max();
            int bucketCount = 100;
            double bucketSize = (max - min) / bucketCount;

            // Histogram
            var histogramSeries = new HistogramSeries
            {
                Title = "Histogram",
                FillColor = OxyColors.SkyBlue,
                StrokeColor = OxyColors.Black,
                StrokeThickness = 1
            };

            // Percentile line (unchanged)
            var percentileSeries = new LineSeries
            {
                Title = "Percentile",
                Color = OxyColors.Red,
                YAxisKey = "PercentileAxis"
            };

            var partial = 0;

            for (int i = 0; i < bucketCount; i++)
            {
                double bucketStart = min + i * bucketSize;
                double bucketEnd = bucketStart + bucketSize;
                int count = data.Count(v => v >= bucketStart && v < bucketEnd);
                if (i == bucketCount - 1)
                    count += data.Count(v => v == max);
                partial += count;

                double area = count * (double)data.Length;
                histogramSeries.Items.Add(new HistogramItem(bucketStart, bucketEnd, area, count));
                percentileSeries.Points.Add(new DataPoint(bucketStart + (bucketSize / 2), ((double) partial) / ((double) data.Length) * 100));
            }

            // Plot model
            var model = new PlotModel();

            // X Axis
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = min,
                Maximum = max,
                Title = "Value",
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
            var minPecentile = _model?.Properties.MinPercentile ?? 10;
            var maxPercentile = _model?.Properties.MaxPercentile ?? 90;
            var lineMinPercentile = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = minPecentile,
                Color = OxyColors.Green,
                LineStyle = LineStyle.Dash,
                Text = $"{minPecentile}th Percentile",
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
    }
}