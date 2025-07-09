namespace QRiskTree.Engine.Model
{
    /// <summary>
    /// Types of contact.
    /// </summary>
    public enum ContactType
    {
        /// <summary>
        /// Represents an undefined state or value.
        /// </summary>
        /// <remarks>This member is used to indicate a state or value that has not been defined or
        /// initialized. It can be useful in scenarios where a default or placeholder value is needed.</remarks>
        Undefined,

        /// <summary>
        /// A random contact happens by chance. There is no intentionality.
        /// </summary>
        Random,

        /// <summary>
        /// A regular contact occurs as part of the normal behavior.
        /// </summary>
        Regular,

        /// <summary>
        /// An intentional contact occurs due to some intention by the subject.
        /// </summary>
        Intentional
    }
}
