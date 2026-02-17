using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Security;
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
    /// Interaction logic for SinglePassword.xaml
    /// </summary>
    public partial class SinglePassword : Window
    {
        public SinglePassword()
        {
            InitializeComponent();
            _password.Focus();
        }

        public SecureString Password => _password.SecurePassword;

        private void _ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
