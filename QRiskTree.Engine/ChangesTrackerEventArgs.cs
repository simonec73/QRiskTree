namespace QRiskTree.Engine
{
    public class ChangesTrackerEventArgs : EventArgs
    {
        public ChangesTrackerEventArgs(ChangesTracker tracker, EventArgs args)
        {
            InnerTracker = tracker;
            InnerArgs = args;
        }

        public ChangesTracker InnerTracker { get; private set; }
        public EventArgs InnerArgs { get; private set; }
    }
}
