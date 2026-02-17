using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// Interaction logic for DoublePassword.xaml
    /// </summary>
    public partial class DoublePassword : Window
    {
        public DoublePassword()
        {
            InitializeComponent();
            _password.Focus();
        }

        public SecureString Password => _password.SecurePassword;

        private void _password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _passwordQuality.Value = _password.SecurePassword.GetScore();
        }

        private void _passwordRepeat_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _ok.IsEnabled = SecureStringEquals(_password.SecurePassword, _passwordRepeat.SecurePassword);
        }

        private void _ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private static bool SecureStringEquals(SecureString ss1, SecureString ss2)
        {
            if (ss1 == null || ss2 == null)
                return ss1 == ss2;

            if (ss1.Length != ss2.Length)
                return false;

            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;

            try
            {
                bstr1 = Marshal.SecureStringToBSTR(ss1);
                bstr2 = Marshal.SecureStringToBSTR(ss2);

                int length = ss1.Length;
                for (int i = 0; i < length; i++)
                {
                    char c1 = (char)Marshal.ReadInt16(bstr1, i * 2);
                    char c2 = (char)Marshal.ReadInt16(bstr2, i * 2);

                    if (c1 != c2)
                        return false;
                }

                return true;
            }
            finally
            {
                if (bstr1 != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr1);
                if (bstr2 != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr2);
            }
        }
    }
}
