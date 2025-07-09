using Newtonsoft.Json;

namespace QRiskTree.Engine
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

        [JsonProperty("createdBy", Order = 100)]
        public string? CreatedBy { get; protected set; }

        [JsonProperty("createdOn", Order = 101)]
        public DateTime CreatedOn { get; protected set; }

        [JsonProperty("modifiedBy", Order = 102)]
        public string? ModifiedBy { get; protected set; }

        [JsonProperty("modifiedOn", Order = 103)]
        public DateTime ModifiedOn { get; protected set; }
    }
}
