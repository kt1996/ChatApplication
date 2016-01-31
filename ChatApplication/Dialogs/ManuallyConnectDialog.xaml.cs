using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
                passwordBox.MaxLength = 40;
                passwordBox.MaxWidth = 180;
                passwordBox.KeyDown += new System.Windows.Input.KeyEventHandler(IPTextBoxKeyDown);
                passwordBox.ToolTipOpening += new ToolTipEventHandler(PasswordBoxToolTipOpening);
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
                passwordBox.MaxWidth = 180;
                passwordBox.KeyDown += new System.Windows.Input.KeyEventHandler(IPTextBoxKeyDown);
                passwordBox.ToolTipOpening += new ToolTipEventHandler(PasswordBoxToolTipOpening);
                passwordGrid.Children.Add(passwordBox);
                passwordBox.MaxLength = 40;
                Grid.SetColumn(passwordBox, 1);
                passwordBox.SelectAll();
                passwordBox.Focus();
            }
            else
            {
                IPTextBox.Text = "Enter IP Address";
                IPTextBox.SelectAll();
                IPTextBox.Focus();
            }
            IPTextBox.Style = Resources["noError"] as Style;
        }

        public string IP
        {
            get { return IPTextBox.Text; }
            set { IPTextBox.Text = value; }
        }

        public string password
        {
            get {
                if (passwordBox != null && passwordBox.Password != "")
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
                passwordBox.KeyDown += new System.Windows.Input.KeyEventHandler(IPTextBoxKeyDown);
                passwordBox.ToolTipOpening += new ToolTipEventHandler(PasswordBoxToolTipOpening);
                passwordBox.MaxLength = 40;
                passwordBox.MaxWidth = 180;
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

        private void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            IPTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            System.Net.IPAddress _ip;
            if (!System.Net.IPAddress.TryParse(IPTextBox.Text, out _ip)) {
                IPTextBox.Style = Resources["noError"] as Style;
                IPTextBox.Style = Resources["blinkingError"] as Style;
            }
            else { 
                DialogResult = true;
            }
        }

        private void IPTextChanged(object sender, TextChangedEventArgs e)
        {
            IPTextBox.Style = Resources["noError"] as Style;
        }

        private void IPTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            PasswordBox _passwordBox = sender as PasswordBox;
            if(_passwordBox.Password.Length == _passwordBox.MaxLength) {
                if (_passwordBox.ToolTip == null) {
                    ToolTip _tooltip = new ToolTip { Content = "Password has max length of " + _passwordBox.MaxLength + " characters" };
                    _tooltip.PlacementTarget = _passwordBox;
                    _tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                    _tooltip.HorizontalOffset = 5;
                    _tooltip.VerticalOffset = 5;
                    _passwordBox.ToolTip = _tooltip;
                    _tooltip.IsOpen = true;
                    _tooltip.StaysOpen = false;
                }
                else {
                    ToolTip _tooltip = (ToolTip)_passwordBox.ToolTip;
                    _tooltip.StaysOpen = true;
                    _tooltip.Visibility = Visibility.Collapsed;
                    _tooltip = new ToolTip { Content = "Password has max length of " + _passwordBox.MaxLength + " characters" };
                    _tooltip.PlacementTarget = _passwordBox;
                    _tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                    _tooltip.HorizontalOffset = 5;
                    _tooltip.VerticalOffset = 5;
                    _passwordBox.ToolTip = _tooltip;
                    _tooltip.IsOpen = true;
                    _tooltip.StaysOpen = false;
                }               
            }
        }

        private void PasswordBoxToolTipOpening(object sender, ToolTipEventArgs e)
        {
            ToolTip _tooltip = (ToolTip)passwordBox.ToolTip;
            _tooltip.StaysOpen = true;
            _tooltip.Visibility = Visibility.Collapsed;
        }
    }

    public class IPValidation : System.ComponentModel.IDataErrorInfo
    {
        public string IP { get; set; }

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get {
                string _result = string.Empty;
                System.Net.IPAddress _ip;
                if (!System.Net.IPAddress.TryParse(IP, out _ip)) {
                    _result = "Invalid IP Address";
                }
                return _result;
            }
        }
    }
}
