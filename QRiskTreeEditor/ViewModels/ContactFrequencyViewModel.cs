using QRiskTree.Engine.Model;
using System.ComponentModel;
using PT = PropertyTools.DataAnnotations;

namespace QRiskTreeEditor.ViewModels
{
    internal class ContactFrequencyViewModel : NodeViewModel
    {
        public ContactFrequencyViewModel(ContactFrequency node, NodeViewModel parent, RiskModelViewModel model)
            : base(node, parent, model)
        {
        }

        [Category("Contact Frequency")]
        [DisplayName("Contact Type")]
        [PT.SelectorStyle(PT.SelectorStyle.ComboBox)]
        [PT.SortIndex(50)]
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
