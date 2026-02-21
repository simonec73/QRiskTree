namespace QRiskTree.Engine.ImportExport
{
    /// <summary>
    /// Interface representing a fact that can be associated with risks and mitigations in the risk model. 
    /// A fact is a piece of information that provides context or evidence for a risk or mitigation. 
    /// It has a name and an optional description, and can be linked to multiple risks and mitigations to provide additional insights into the relationships between them.
    /// </summary>
    /// <remarks>It is mapped to a Number range.</remarks>
    public interface IFact : INamedObject
    {
    }
}
