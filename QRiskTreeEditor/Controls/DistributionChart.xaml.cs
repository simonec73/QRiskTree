using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using QRiskTree.Engine;
using QRiskTreeEditor.ViewModels;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Controls;

namespace QRiskTreeEditor.Controls
{
    public partial class DistributionChart : UserControl
    {
        private RiskModelViewModel? _model;
        private const int _bucketCount = 250;
        private double _bucketSize = 0;

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
                case RelevantEvent.BaselineAndOptimizationTarget:
                    model.Model.BaselineSimulationCompleted += ComparisonBaselineSimulationCompleted;
                    model.Model.OptimalFirstYearSimulationCompleted += ComparisonFirstYearCompleted;
                    model.Model.OptimalFollowingYearsSimulationCompleted += ComparisonFollowingYearsCompleted;
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

        private void ComparisonBaselineSimulationCompleted(double[] samples)
        {
            BaselinePlot(samples);
        }

        private void ComparisonFirstYearCompleted(IEnumerable<Guid>? mitigationIds, double[] samples)
        {
            if (_model != null && !_model.Properties.IgnoreImplementationCosts)
            {
                ComparisonPlot(samples);
            }
        }

        private void ComparisonFollowingYearsCompleted(IEnumerable<Guid>? mitigationIds, double[] samples)
        {
            if (_model != null && _model.Properties.IgnoreImplementationCosts)
            {
                ComparisonPlot(samples);
            }
        }

        private void Plot(double[] data)
        {
            if (data == null || data.Length == 0)
                return;

            double min = data.Min();
            double max = data.Max();
            double bucketSize = (max - min) / _bucketCount;

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

            for (int i = 0; i < _bucketCount; i++)
            {
                double bucketStart = min + i * bucketSize;
                double bucketEnd = bucketStart + bucketSize;
                int count = data.Count(v => v >= bucketStart && v < bucketEnd);
                if (i == _bucketCount - 1)
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
            var minPercentile = _model?.Properties.MinPercentile ?? 10;
            var maxPercentile = _model?.Properties.MaxPercentile ?? 90;
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
            var range = data.ToRange(RangeType.Money, minPercentile, maxPercentile);
            if (range != null)
            {
                var lineMinValue = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = range.Min,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"Value at {minPercentile}th Percentile: {range.Min:N0}",
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top
                };

                var lineMaxValue = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = range.Max,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"Value at {maxPercentile}th Percentile: {range.Max:N0}",
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top
                };

                model.Annotations.Add(lineMinValue);
                model.Annotations.Add(lineMaxValue);
            }

            PlotView.Model = model;
        }

        private void BaselinePlot(double[] data)
        {
            if (data == null || data.Length == 0)
                return;

            double min = data.Min();
            double max = data.Max();
            _bucketSize = (max - min) / _bucketCount;

            // Baseline Histogram
            var histogramSeries = new HistogramSeries
            {
                Title = "Baseline",
                FillColor = OxyColors.SkyBlue,
                StrokeColor = OxyColors.Black,
                StrokeThickness = 1
            };

            for (int i = 0; i < _bucketCount; i++)
            {
                double bucketStart = min + i * _bucketSize;
                double bucketEnd = bucketStart + _bucketSize;
                int count = data.Count(v => v >= bucketStart && v < bucketEnd);
                if (i == _bucketCount - 1)
                    count += data.Count(v => v == max);

                double area = count * (double)data.Length;
                histogramSeries.Items.Add(new HistogramItem(bucketStart, bucketEnd, area, count));
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
                Title = "Baseline Count",
                StringFormat = "N0"
            });

            model.Series.Add(histogramSeries);

            PlotView.Model = model;
        }

        private void ComparisonPlot(double[] data)
        {
            if (data == null || data.Length == 0)
                return;

            var model = PlotView.Model;

            if (model != null)
            {
                var histograms = model.Series.OfType<HistogramSeries>();
                if (histograms.Count() > 1)
                {
                    var first = true;
                    foreach (var histogram in histograms)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            model.Series.Remove(histogram);
                        }
                    }
                }

                double min = data.Min();
                double max = data.Max();

                var xAxis = model.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
                if (xAxis != null)
                {
                    xAxis.Minimum = Math.Min(xAxis.Minimum, min);
                    xAxis.Maximum = Math.Max(xAxis.Maximum, max);
                }

                // Optimized Y Axis
                model.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Right,
                    Key = "OptimizedAxis",
                    Title = "Optimized Count"
                });

                // Optimized Histogram
                var histogramSeries = new HistogramSeries
                {
                    Title = "Optimized",
                    FillColor = OxyColors.Green,
                    StrokeColor = OxyColors.Black,
                    StrokeThickness = 1,
                    YAxisKey = "OptimizedAxis"
                };

                for (int i = 0; i < _bucketCount; i++)
                {
                    double bucketStart = min + i * _bucketSize;
                    double bucketEnd = bucketStart + _bucketSize;
                    int count = data.Count(v => v >= bucketStart && v < bucketEnd);
                    if (i == _bucketCount - 1)
                        count += data.Count(v => v == max);

                    double area = count * (double)data.Length;
                    histogramSeries.Items.Add(new HistogramItem(bucketStart, bucketEnd, area, count));
                }

                model.Series.Add(histogramSeries);

                PlotView.InvalidatePlot(true);
            }
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