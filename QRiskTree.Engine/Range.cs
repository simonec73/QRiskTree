using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRiskTree.Engine.Facts;

namespace QRiskTree.Engine
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Range : ChangesTracker
    {
        #region Constructors.
        private Range() : base()
        {

        }

        public Range(RangeType rangeType) : base()
        {
            RangeType = rangeType;
        }

        /// <summary>
        /// Internal constructor used to clone a Range object.
        /// </summary>
        /// <param name="range">Source range object</param>
        /// <exception cref="ArgumentNullException">The source range object cannot be null.</exception>
        internal Range(Range range) : base()
        {
            if (range == null)
                throw new ArgumentNullException(nameof(range));
            RangeType = range.RangeType;
            _min = range.Min;
            _mode = range.Mode;
            _max = range.Max;
            _confidence = range.Confidence;
            _calculated = range.Calculated;
        }

        public Range(RangeType rangeType, double min, double mode, double max, Confidence confidence)
            : base()
        {
            RangeType = rangeType;

            if (!IsValid(min))
                throw new ArgumentOutOfRangeException("Min", $"Value '{min}' is not acceptable for {RangeType}.");
            if (!IsValid(mode))
                throw new ArgumentOutOfRangeException("mode", $"Value '{mode}' is not acceptable for {RangeType}.");
            if (!IsValid(max))
                throw new ArgumentOutOfRangeException("Max", $"Value '{max}' is not acceptable for {RangeType}.");
            if (!IsValid(min, max))
                throw new ArgumentException("Invalid range values: Min must be less than or equal to Max.");

            _min = min;
            _mode = mode;
            _max = max;
            _confidence = confidence;
        }
        #endregion

        #region Static methods.
        public static double GetMinAllowed(RangeType rangeType)
        {
            return 0.0; // Minimum value is always 0 for all range types.
        }

        public static double GetMaxAllowed(RangeType rangeType)
        {
            switch (rangeType)
            {
                case RangeType.Money:
                    return double.MaxValue; // No upper limit for money
                case RangeType.Frequency:
                    return 525600; // Maximum frequency is 525600 (once per minute, during the whole year)
                case RangeType.Percentage:
                    return 1.0; // Maximum percentage is 100%
                default:
                    throw new ArgumentOutOfRangeException(nameof(rangeType), $"Unsupported range type: {rangeType}");
            }
        }
        #endregion

        #region Public methods.
        public Range Set(double min, double mode, double max, Confidence confidence)
        {
            if (!IsValid(min))
                throw new ArgumentOutOfRangeException(nameof(min), $"Value '{min}' is not acceptable for {RangeType}.");
            if (!IsValid(mode))
                throw new ArgumentOutOfRangeException(nameof(mode), $"Value '{mode}' is not acceptable for {RangeType}.");
            if (!IsValid(max))
                throw new ArgumentOutOfRangeException(nameof(max), $"Value '{max}' is not acceptable for {RangeType}.");

            _min = min;
            _mode = mode;
            _max = max;
            _confidence = confidence;
            _calculated = false;
            Update();

            return this;
        }

        public T Set<T>(double min, double mode, double max, Confidence confidence) where T : Range
        {
            return (T) Set(min, mode, max, confidence);
        }

        /// <summary>
        /// Resets the range to its default values.
        /// </summary>
        public void Reset()
        {
            _min = GetMinAllowed(RangeType);
            _mode = _min;
            _max = GetMinAllowed(RangeType);
            _confidence = Confidence.Low;
            _calculated = null;
            Update();
        }
        #endregion

        #region Properties.
        [JsonProperty("calculated", Order = 10)]
        protected bool? _calculated = null;

        /// <summary>
        /// Flag howing if the node values (Min, Max, Mode and Confidence) are calculated or not.
        /// </summary>
        /// <remarks>If it is set by a caller, it is set to false.
        /// If it is calculated, it is set to true.
        /// In every other case, it is null.</remarks>
        public bool? Calculated => _calculated;

        /// <summary>
        /// Type of the range. It is determined by the derived class.
        /// </summary>
        public readonly RangeType RangeType;

        [JsonProperty("Min", Order = 11)]
        protected double _min { get; set; } = 0.0;

        /// <summary>
        /// 10th Percentile.
        /// </summary>
        public double Min
        {
            get => _min;
            set
            {
                if (value != _min)
                {
                    if (!IsValid(value))
                        throw new ArgumentOutOfRangeException("value", $"Value '{value}' is not acceptable for {RangeType}.");

                    _min = value;
                    _calculated = false;
                    Update();
                }
            }
        }

        [JsonProperty("mode", Order = 12)]
        protected double _mode { get; set; } = 0.0;

        /// <summary>
        /// Mode value.
        /// </summary>
        public double Mode
        {
            get => _mode;
            set
            {
                if (value != _mode)
                {
                    if (!IsValid(value))
                        throw new ArgumentOutOfRangeException("value", $"Value '{value}' is not acceptable for {RangeType}.");

                    _mode = value;
                    _calculated = false;
                    Update();
                }
            }
        }

        [JsonProperty("Max", Order = 13)]
        protected double _max { get; set; } = 0.0;

        /// <summary>
        /// 90th Percentile.
        /// </summary>
        public double Max
        {
            get => _max;
            set
            {
                if (value != _max)
                {
                    if (!IsValid(value))
                        throw new ArgumentOutOfRangeException("value", $"Value '{value}' is not acceptable for {RangeType}.");

                    _max = value;
                    _calculated = false;
                    Update();
                }
            }
        }

        [JsonProperty("confidence", Order = 14)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        protected Confidence _confidence { get; set; } = Confidence.Low;

        /// <summary>
        /// Confidence.
        /// </summary>
        public Confidence Confidence
        {
            get => _confidence;
            set
            {
                if (value != _confidence)
                {
                    _confidence = value;
                    _calculated = false;
                    Update();
                }
            }
        }
        #endregion

        #region Private methods: Validation.
        private bool IsValid(double min, double max)
        {
            return min <= max;
        }

        private bool IsValid(double value)
        {
            return value >= GetMinAllowed(RangeType) && value <= GetMaxAllowed(RangeType);
        }
        #endregion
    }
}