using Newtonsoft.Json;

namespace QRiskTree.Engine.Facts
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ChangesTracker
    {
        public ChangesTracker()
        {
            CreatedBy = UserName.GetDisplayName();
            CreatedOn = DateTime.Now;
        }

        public event EventHandler? Changed;

        internal virtual void Update()
        {
            ModifiedBy = UserName.GetDisplayName();
            ModifiedOn = DateTime.Now;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        internal void Update(ChangesTracker tracker, EventArgs eventArgs)
        {
            ModifiedBy = UserName.GetDisplayName();
            ModifiedOn = DateTime.Now;
            Changed?.Invoke(this, new ChangesTrackerEventArgs(tracker, eventArgs));
        }

        [JsonProperty("createdBy")]
        public string? CreatedBy { get; protected set; }

        [JsonProperty("createdOn")]
        public DateTime CreatedOn { get; protected set; }

        [JsonProperty("modifiedBy")]
        public string? ModifiedBy { get; protected set; }

        [JsonProperty("modifiedOn")]
        public DateTime ModifiedOn { get; protected set; }
    }
}
