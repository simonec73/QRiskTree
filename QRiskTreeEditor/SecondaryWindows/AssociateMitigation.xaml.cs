using QRiskTreeEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QRiskTreeEditor.SecondaryWindows
{
    /// <summary>
    /// Interaction logic for AssociateMitigation.xaml
    /// </summary>
    public partial class AssociateMitigation : Window
    {
        public AssociateMitigation()
        {
            InitializeComponent();
        }

        internal AssociateMitigation(MitigatedRiskViewModel risk, IEnumerable<MitigationCostViewModel> mitigations) : this()
        {
            _parent.Text = risk.Name;
            _items.ItemsSource = mitigations;
        }

        internal MitigationCostViewModel? SelectedMitigation => _items.SelectedItem as MitigationCostViewModel;

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
