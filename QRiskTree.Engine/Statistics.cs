using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace QRiskTree.Engine
{
    internal static class Statistics
    {
        public static Range? ToRange(this double[]? samples, RangeType rangeType, Confidence confidence = Confidence.Moderate)
        {
            Range? result = null;

            if ((samples?.Length ?? 0) > 0)
            {
                var perc10 = samples.Percentile(10);
#pragma warning disable CS8604 // Possible null reference argument.
                var mode = samples.CalculateMode();
#pragma warning restore CS8604 // Possible null reference argument.
                var perc90 = samples.Percentile(90);
                result = new Range(rangeType, perc10, mode, perc90, confidence);
            }

            return result;
        }

        public static bool GenerateSamples(this Range range, uint iterations, out double[]? samples)
        {
            return GenerateSamples(range.Perc10, range.Mode, range.Perc90, range.Confidence, iterations, out samples);
        }

        public static bool GenerateSamples(double perc10, double mode, double perc90, Confidence confidence,
           uint iterations, out double[]? samples)
        {
            var result = false;
            samples = null;

            var pert = GetPertDistribution(perc10, mode, perc90, confidence);
            if (pert != null)
            {
                samples = new double[iterations];
                pert.Samples(samples);
                result = true;
            }

            return result;
        }

        private static BetaScaled? GetPertDistribution(double perc10, double mode, double perc90, Confidence confidence)
        {
            var modepad = 0.000000001;
            var lambda = CalculateLambda(confidence);
            var mean = (perc10 + lambda * mode + perc90) / (lambda + 2.0);
            var effectiveMode = mode;
            if (mode - mean == 0.0)
            {
                effectiveMode += modepad;
                mean = (perc10 + lambda * effectiveMode + perc90) / (lambda + 2.0);
            }

            var alpha = ((mean - perc10) * ((2.0 * effectiveMode) - perc10 - perc90)) /
                        ((effectiveMode - mean) * (perc90 - perc10));
            var beta = (alpha * (perc90 - mean)) / (mean - perc10);

            if (BetaScaled.IsValidParameterSet(alpha, beta, perc10, perc90 - perc10))
                return new BetaScaled(alpha, beta, perc10, perc90 - perc10);
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

        public static double CalculateMode(this double[] data, uint binsCount = 0)
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
                if (bin == binsCount) bin--; // Edge case: max value falls into the last bin
                if (bin >= 0 && bin < binsCount)
                    bins[bin]++;
            }

            int maxBin = Array.IndexOf(bins, bins.Max());
            return binCenters[maxBin];
        }
    }
}
