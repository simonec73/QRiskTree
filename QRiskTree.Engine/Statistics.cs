using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using System.ComponentModel.DataAnnotations;

namespace QRiskTree.Engine
{
    /// <summary>
    /// Utility class for statistical calculations.
    /// </summary>
    public static class Statistics
    {
        /// <summary>
        /// Converts an array of samples into a Range object based on specified percentiles.
        /// </summary>
        /// <param name="samples">Samples to be converted into a Range.</param>
        /// <param name="rangeType">Type of range to be created.</param>
        /// <param name="minPercentile">Minimum percentile to define the range.</param>
        /// <param name="maxPercentile">Maximum percentile to define the range.</param>
        /// <returns>A Range object representing the specified percentiles of the samples.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Percentiles must be between 0 and 100.</exception>
        public static Range? ToRange(this double[]? samples, RangeType rangeType, int minPercentile, int maxPercentile)
        {
            if (minPercentile < 0 || minPercentile > 100)
                throw new ArgumentOutOfRangeException(nameof(minPercentile));
            if (maxPercentile < 0 || maxPercentile > 100)
                throw new ArgumentOutOfRangeException(nameof(maxPercentile));

            Range? result = null;

            if ((samples?.Length ?? 0) > 0)
            {
                var min = samples.Percentile(minPercentile);
#pragma warning disable CS8604 // Possible null reference argument.
                var mode = samples.CalculateMode();
#pragma warning restore CS8604 // Possible null reference argument.
                var max = samples.Percentile(maxPercentile);
                var confidence = samples.CalculateConfidence();
                result = new Range(rangeType, min, mode, max, confidence);
            }

            return result;
        }

        internal static bool GenerateSamples(this Range range, uint iterations, out double[]? samples)
        {
            return GenerateSamples(range.Min, range.Mode, range.Max, range.Confidence, iterations, out samples);
        }

        internal static bool GenerateSamples(double min, double mode, double max, Confidence confidence,
           uint iterations, out double[]? samples)
        {
            var result = false;
            samples = null;

            var pert = GetPertDistribution(min, mode, max, confidence);
            if (pert != null)
            {
                samples = new double[iterations];
                pert.Samples(samples);
                result = true;
            }

            return result;
        }

        private static BetaScaled? GetPertDistribution(double min, double mode, double max, Confidence confidence)
        {
            var modepad = 0.000000001;
            var lambda = CalculateLambda(confidence);
            var mean = (min + lambda * mode + max) / (lambda + 2.0);
            var effectiveMode = mode;
            if (mode - mean == 0.0)
            {
                effectiveMode += modepad;
                mean = (min + lambda * effectiveMode + max) / (lambda + 2.0);
            }

            var alpha = ((mean - min) * ((2.0 * effectiveMode) - min - max)) /
                        ((effectiveMode - mean) * (max - min));
            var beta = (alpha * (max - mean)) / (mean - min);

            if (BetaScaled.IsValidParameterSet(alpha, beta, min, max - min))
                return new BetaScaled(alpha, beta, min, max - min);
            else
                return null;
        }

        private static int CalculateLambda(Confidence confidence)
        {
            var result = 1;

            switch (confidence)
            {
                case Confidence.Low:
                    result = 4;
                    break;
                case Confidence.Moderate:
                    result = 20;
                    break;
                case Confidence.High:
                    result = 160;
                    break;
            }

            return result;
        }

        internal static double CalculateMode(this double[] data, uint binsCount = 0)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data array is null or empty.", nameof(data));
            if (binsCount == 0)
            {
                if (data.Length < 10000)
                {
                    binsCount = (uint)data.Length / 100; // Default to 1% of the data length
                }
                else
                {
                    binsCount = (uint)data.Length / 1000; // Default to 0.1% of the data length
                }
            }

            double min = data.Min();
            double max = data.Max();
            if (min == max)
                return min; // All values are the same

            double binWidth = (max - min) / binsCount;
            int[] bins = new int[(int)binsCount];
            double[] binCenters = new double[(int)binsCount];

            for (int i = 0; i < binsCount; i++)
                binCenters[i] = min + (i + 0.5) * binWidth;

            foreach (var value in data)
            {
                int bin = (int)((value - min) / binWidth);
                if (bin == binsCount) bin--; // Edge case: Max value falls into the last bin
                if (bin >= 0 && bin < binsCount)
                    bins[bin]++;
            }

            int maxBin = Array.IndexOf(bins, bins.Max());
            return binCenters[maxBin];
        }

        internal static Confidence CalculateConfidence(this double[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data array is null or empty.", nameof(data));

            // Calculate kurtosis (peakedness) of the distribution
            var kurtosis = data.Kurtosis();

            // Map kurtosis to confidence levels
            // Higher kurtosis = more peaked = higher confidence
            // Negative kurtosis (platykurtic) = flatter = lower confidence
            if (kurtosis < -0.5)
                return Confidence.Low;
            else if (kurtosis < 1.5)
                return Confidence.Moderate;
            else
                return Confidence.High;
        }
    }
}
