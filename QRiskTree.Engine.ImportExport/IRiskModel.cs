namespace QRiskTree.Engine.ImportExport
{
    /// <summary>
    /// Interface representing a risk model that can be imported into.
    /// </summary>
    public interface IRiskModel<R, M> where R : INamedObject where M : INamedObject
    {
        /// <summary>
        /// Adds a new risk to the model with the specified name and returns the created risk object.
        /// </summary>
        /// <param name="name">The name of the risk to add.</param>
        /// <returns>The created risk object.</returns>
        R AddRisk(string name);

        /// <summary>
        /// Adds a new mitigation to the model with the specified name and returns the created mitigation object.
        /// </summary>
        /// <param name="name">The name of the mitigation to add.</param>
        /// <returns>The created mitigation object.</returns>
        M AddMitigation(string name);

        /// <summary>
        /// Retrieves a risk from the model by its name. Returns null if no risk with the specified name exists.
        /// </summary>
        /// <param name="name">The name of the risk to retrieve.</param>
        /// <returns>The risk object if found; otherwise, null.</returns>
        R? GetRisk(string name);

        /// <summary>
        /// Retrieves a mitigation object with the specified name.
        /// </summary>
        /// <param name="name">The name of the mitigation to retrieve.</param>
        /// <returns>An object representing the mitigation with the specified name.</returns>
        M? GetMitigation(string name);

        /// <summary>
        /// Add a fact to the model with the specified name and description, and returns the created fact object.
        /// </summary>
        /// <param name="context">The context of the fact to add.</param>
        /// <param name="source">The source of the fact to add.</param>
        /// <param name="name">The name of the fact to add.</param>
        /// <param name="description">The description of the fact to add.</param>
        /// <returns>The created fact object.</returns>
        IFact? AddFact(string context, string source, string name, string? description);
    }
}
