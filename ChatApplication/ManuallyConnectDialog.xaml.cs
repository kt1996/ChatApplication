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

namespace ChatApplication
{
    /// <summary>
    /// Interaction logic for ManuallyConnectDialog.xaml
    /// </summary>
    public partial class ManuallyConnectDialog : Window
    {
        public PasswordBox passwordBox = null;

        public ManuallyConnectDialog(string IP = null, string nick = null, string message = null)
        {
            InitializeComponent();
            if(message != null)
            {
                Grid.SetRow(responsePanel, 3);
                Label _messageLabel = new Label();
                _messageLabel.Content = message;
                _messageLabel.Foreground = Brushes.Red;
                _messageLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
                _messageLabel.Margin = new Thickness(0, 10, 0, -10);
                mainGrid.Children.Add(_messageLabel);
                Grid.SetRow(_messageLabel, 2);
            }
            if(IP != null)
            {
                IPTextBox.Text = IP;
                IPTextBox.IsEnabled = false;

                passwordRequired.IsChecked = true;
                passwordRequired.IsEnabled = false;

                passwordBox = new PasswordBox();
                passwordBox.Name = "passwordBox";
                passwordBox.Margin = new Thickness(3, 4, 0, -7);
                passwordGrid.Children.Add(passwordBox);
                Grid.SetColumn(passwordBox, 1);
                passwordBox.SelectAll();
                passwordBox.Focus();
            }
            if(nick != null)
            {               
                IPTextBox.Width = 150;
                Label _label = new Label();
                if(nick.Length > 10)
                {
                    nick = nick.Substring(0, 8) + "..";
                }
                nick = "(" + nick + ")";
                _label.Content = nick;
                _label.Width = 1000;                
                IPPanel.Children.Add(_label);
                Grid.SetColumnSpan(IPTextBox, 1);
                Grid.SetColumn(_label, 1);
                passwordRequired.IsChecked = true;
                passwordRequired.IsEnabled = false;

                passwordBox = new PasswordBox();
                passwordBox.Name = "passwordBox";
                passwordBox.Margin = new Thickness(3, 4, 0, -7);
                passwordGrid.Children.Add(passwordBox);
                Grid.SetColumn(passwordBox, 1);
                passwordBox.SelectAll();
                passwordBox.Focus();
            }
            else
            {
                IPTextBox.SelectAll();
                IPTextBox.Focus();
            }            
        }

        public string IP
        {
            get { return IPTextBox.Text; }
            set { IPTextBox.Text = value; }
        }

        public string password
        {
            get {
                if (passwordBox != null)
                {
                    return passwordBox.Password;
                }
                else
                {
                    return null;
                }
            }
            set { if (passwordBox != null) { passwordBox.Password = value; } }
        }


        private void CheckBoxClicked(object sender, RoutedEventArgs e)
        {
            if ((bool)passwordRequired.IsChecked)
            {
                passwordBox = new PasswordBox();
                passwordBox.Name = "passwordBox";
                passwordBox.Margin = new Thickness(3, 4, 0, -7);
                passwordGrid.Children.Add(passwordBox);
                Grid.SetColumn(passwordBox, 1);
                passwordBox.SelectAll();
                passwordBox.Focus();
            }
            else
            {
                passwordGrid.Children.Remove(passwordBox);
                passwordBox = null;
                IPTextBox.SelectAll();
                IPTextBox.Focus();
            }
        }

        private void OKButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
