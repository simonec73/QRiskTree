using QRiskTree.Engine.Model;

namespace QRiskTreeEditor.ViewModels
{
    internal class ContactFrequencyViewModel : NodeViewModel
    {
        public ContactFrequencyViewModel(ContactFrequency node, NodeViewModel parent, RiskModelViewModel model)
            : base(node, parent, model)
        {
        }

        public ContactType ContactType
        {
            get => (_node as ContactFrequency)?.ContactType ?? ContactType.Undefined;
            set
            {                 
                if (_node is ContactFrequency contactFrequency && contactFrequency.ContactType != value)
                {
                    contactFrequency.ContactType = value;
                    OnPropertyChanged(nameof(ContactType));
                }
            }
        }

    }
}
