namespace QRiskTree.Engine
{
    /// <summary>
    /// Interface implemented by the Simulation Containers.
    /// </summary>
    /// <remarks>Simulation Containers are used to get the results of a simulation.</remarks>
    public interface ISimulationContainer
    {
        /// <summary>
        /// Register the samples generated for a given node.
        /// </summary>
        /// <param name="node">Node that has been simulated.</param>
        /// <param name="samples">Samples generated for the node.</param>
        void AddSimulation(Node node, double[] samples);

        /// <summary>
        /// Get the samples generated for a given node.
        /// </summary>
        /// <param name="node">Node that has been simulated.</param>
        /// <returns>Samples generated for the node.</returns>
        double[]? GetSimulation(Node node);
    }
}
