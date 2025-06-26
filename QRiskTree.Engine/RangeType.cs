namespace QRiskTree.Engine
{
    public enum RangeType
    {
        /// <summary>
        /// The range is a monetary value.
        /// </summary>
        /// <remarks>Acceptable values are between 0 and Double.Max.</remarks>
        Money,
        /// <summary>
        /// The range is represented as frequency, expressed in times per year.
        /// </summary>
        /// <remarks>Acceptable values are between 0 (never) and 525600 (certainty, once per minute).</remarks>
        Frequency,
        /// <summary>
        /// The range is a percentage value.
        /// </summary>
        /// <remarks>Acceptable values are from 0.0 (0%) to 1.0 (100%).</remarks>
        Percentage
    }
}
