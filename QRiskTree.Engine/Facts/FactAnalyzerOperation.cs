namespace QRiskTree.Engine.Facts
{
    /// <summary>
    /// Operation to perform in a FactAnalyzerNode.
    /// </summary>
    public enum FactAnalyzerOperation
    {
        /// <summary>
        /// Sum the samples generated from the linked Facts or child nodes.
        /// </summary>
        Sum,
        
        /// <summary>
        /// Multiply the samples generated from the linked Facts or child nodes.
        /// </summary>
        Multiply,

        /// <summary>
        /// Average the samples generated from the linked Facts or child nodes.
        /// </summary>
        Average,

        /// <summary>
        /// Get the minimum sample generated from the linked Facts or child nodes.
        /// </summary>
        Min,

        /// <summary>
        /// Get the maximum sample generated from the linked Facts or child nodes.
        /// </summary>
        Max
    }
}
