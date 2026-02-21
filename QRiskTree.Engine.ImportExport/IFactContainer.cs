namespace QRiskTree.Engine.ImportExport
{
    /// <summary>
    /// Interface representing a container for facts that can be associated with risks and mitigations in the risk model.
    /// </summary>
    public interface IFactContainer
    {
        /// <summary>
        /// Adds a  fact to the container.
        /// </summary>
        /// <param name="fact">Fact to be added to the container.</param>
        /// <returns>True if the fact was added successfully; otherwise, false.</returns>
        bool AddFact(IFact fact);
    }
}
