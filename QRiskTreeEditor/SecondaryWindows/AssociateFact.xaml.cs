using QRiskTreeEditor.ViewModels;
using System.Windows;

namespace QRiskTreeEditor.SecondaryWindows
{
    /// <summary>
    /// Interaction logic for AssociateFact.xaml
    /// </summary>
    public partial class AssociateFact : Window
    {
        public AssociateFact()
        {
            InitializeComponent();
        }

        internal AssociateFact(NodeViewModel node, IEnumerable<FactViewModel> facts) : this()
        {
            _parent.Text = node.Name;
            _items.ItemsSource = facts;
        }

        internal FactViewModel? SelectedFact => _items.SelectedItem as FactViewModel;

        private void _ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void _cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
