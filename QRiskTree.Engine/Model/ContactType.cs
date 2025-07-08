namespace QRiskTree.Engine.Model
{
    /// <summary>
    /// Types of contact.
    /// </summary>
    public enum ContactType
    {
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
