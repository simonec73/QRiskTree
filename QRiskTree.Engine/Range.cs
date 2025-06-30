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

        public Range(RangeType rangeType, double perc10, double mode, double perc90, Confidence confidence)
            : base()
        {
            RangeType = rangeType;

            if (!IsValid(perc10))
                throw new ArgumentOutOfRangeException("perc10", $"Value '{perc10}' is not acceptable for {RangeType}.");
            if (!IsValid(mode))
                throw new ArgumentOutOfRangeException("mode", $"Value '{mode}' is not acceptable for {RangeType}.");
            if (!IsValid(perc90))
                throw new ArgumentOutOfRangeException("perc90", $"Value '{perc90}' is not acceptable for {RangeType}.");
            if (!IsValid(perc10, perc90))
                throw new ArgumentException("Invalid range values: Perc10 must be less than or equal to Perc90.");

            _perc10 = perc10;
            _mode = mode;
            _perc90 = perc90;
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
        public Range Set(double perc10, double mode, double perc90, Confidence confidence)
        {
            if (!IsValid(perc10))
                throw new ArgumentOutOfRangeException(nameof(perc10), $"Value '{perc10}' is not acceptable for {RangeType}.");
            if (!IsValid(mode))
                throw new ArgumentOutOfRangeException(nameof(mode), $"Value '{mode}' is not acceptable for {RangeType}.");
            if (!IsValid(perc90))
                throw new ArgumentOutOfRangeException(nameof(perc90), $"Value '{perc90}' is not acceptable for {RangeType}.");

            _perc10 = perc10;
            _mode = mode;
            _perc90 = perc90;
            _calculated = false;
            Update();

            return this;
        }
        #endregion

        #region Properties.
        [JsonProperty("calculated", Order = 10)]
        protected bool? _calculated = null;

        /// <summary>
        /// Flag howing if the node values (Perc10, Perc90, Mode and Confidence) are calculated or not.
        /// </summary>
        /// <remarks>If it is set by a caller, it is set to false.
        /// If it is calculated, it is set to true.
        /// In every other case, it is null.</remarks>
        public bool? Calculated => _calculated;

        /// <summary>
        /// Type of the range. It is determined by the derived class.
        /// </summary>
        public readonly RangeType RangeType;

        [JsonProperty("perc10", Order = 11)]
        protected double _perc10 { get; set; } = 0.0;

        /// <summary>
        /// 10th Percentile.
        /// </summary>
        public double Perc10
        {
            get => _perc10;
            set
            {
                if (value != _perc10)
                {
                    if (!IsValid(value))
                        throw new ArgumentOutOfRangeException("value", $"Value '{value}' is not acceptable for {RangeType}.");

                    _perc10 = value;
                    _calculated = false;
                    Update();
                }
            }
        }

        [JsonProperty("mode", Order = 12)]
        protected double _mode { get; set; } = 0.0;

        /// <summary>
        /// Most Probable value.
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

        [JsonProperty("perc90", Order = 13)]
        protected double _perc90 { get; set; } = 0.0;

        /// <summary>
        /// 90th Percentile.
        /// </summary>
        public double Perc90
        {
            get => _perc90;
            set
            {
                if (value != _perc90)
                {
                    if (!IsValid(value))
                        throw new ArgumentOutOfRangeException("value", $"Value '{value}' is not acceptable for {RangeType}.");

                    _perc90 = value;
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
        private bool IsValid(double perc10, double perc90)
        {
            return perc10 <= perc90;
        }

        private bool IsValid(double value)
        {
            return value >= GetMinAllowed(RangeType) && value <= GetMaxAllowed(RangeType);
        }
        #endregion
    }
}