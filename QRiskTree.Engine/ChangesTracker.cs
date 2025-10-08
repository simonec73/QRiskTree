using Newtonsoft.Json;

namespace QRiskTree.Engine
{
    /// <summary>
    /// Tracks changes to an object, including creation and modification metadata.
    /// </summary>
    /// <remarks>The <see cref="ChangesTracker"/> class provides functionality to track the creation and
    /// modification details of an object, including the user responsible for these actions and the corresponding
    /// timestamps. It also supports notifying subscribers when changes occur through the <see cref="Changed"/>
    /// event.</remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public class ChangesTracker
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ChangesTracker()
        {
            CreatedBy = UserName.GetDisplayName();
            CreatedOn = DateTime.UtcNow;
        }

        public event EventHandler? Changed;

        internal virtual void Update()
        {
            ModifiedBy = UserName.GetDisplayName();
            ModifiedOn = DateTime.UtcNow;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        internal void Update(ChangesTracker tracker, EventArgs eventArgs)
        {
            ModifiedBy = UserName.GetDisplayName();
            ModifiedOn = DateTime.UtcNow;
            Changed?.Invoke(this, new ChangesTrackerEventArgs(tracker, eventArgs));
        }

        /// <summary>
        /// Gets the name of the user who created this object.
        /// </summary>
        [JsonProperty("createdBy", Order = 100)]
        public string? CreatedBy { get; protected set; }

        /// <summary>
        /// Gets the date and time (in UTC) when this object was created.
        /// </summary>
        [JsonProperty("createdOn", Order = 101)]
        public DateTime CreatedOn { get; protected set; }

        /// <summary>
        /// Gets the identifier of the user who last modified the entity.
        /// </summary>
        [JsonProperty("modifiedBy", Order = 102)]
        public string? ModifiedBy { get; protected set; }

        /// <summary>
        /// Gets the date and time when the entity was last modified.
        /// </summary>
        [JsonProperty("modifiedOn", Order = 103)]
        public DateTime ModifiedOn { get; protected set; }
    }
}
