using System.Windows;
using System.Windows.Controls;

namespace ChatApplication
{
    public partial class EditPasswordWindow : Window
    {            
        public EditPasswordWindow(bool PasswordSet)
        {
            InitializeComponent();
            if (PasswordSet) {
                PasswordTextBox.IsEnabled = true;
                PasswordTextBox.Password = "";
                ConfirmPasswordTextBox.IsEnabled = true;
                ConfirmPasswordTextBox.Password = "";
                PasswordTextBox.Focus();
                PasswordTextBox.Style = Resources["errorTemplate"] as Style;
                ConfirmPasswordTextBox.Style = Resources["errorTemplate"] as Style;
                passwordNotRequired.IsChecked = false;
            }
        }

        private void CheckBoxClicked(object sender, RoutedEventArgs e)
        {
            if ((bool)(sender as CheckBox).IsChecked) {
                PasswordTextBox.IsEnabled = false;
                PasswordTextBox.Password = "";
                ConfirmPasswordTextBox.IsEnabled = false;
                ConfirmPasswordTextBox.Password = "";
                ConfirmPasswordTextBox.Style = Resources["noError"] as Style;
                PasswordTextBox.Style = Resources["noError"] as Style;
            }
            else {
                PasswordTextBox.Style = Resources["errorTemplate"] as Style;
                ConfirmPasswordTextBox.Style = Resources["errorTemplate"] as Style;
                PasswordTextBox.IsEnabled = true;
                PasswordTextBox.Password = "";
                ConfirmPasswordTextBox.IsEnabled = true;
                ConfirmPasswordTextBox.Password = "";
                PasswordTextBox.Focus();
            }
        }

        public string Password
        {
            get {
                if ((bool)passwordNotRequired.IsChecked) {
                    return null;
                }
                else {
                    byte[] hash = ((System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.CryptoConfig.CreateFromName("MD5")).ComputeHash(new System.Text.UTF8Encoding().GetBytes(PasswordTextBox.Password));
                    return System.BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                }                
            }
        }

        private void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!(bool)passwordNotRequired.IsChecked) {
                if (PasswordTextBox.Password == ConfirmPasswordTextBox.Password && PasswordTextBox.Password != "") {
                    DialogResult = true;
                }
                else {
                    PasswordTextBox.Style = Resources["blinkingError"] as Style;
                    ConfirmPasswordTextBox.Style = Resources["blinkingError"] as Style;

                    ToolTip _tooltip;
                    if (PasswordTextBox.Password == "") {
                        _tooltip = new ToolTip { Content = "Password cannot be blank" };
                        _tooltip.PlacementTarget = PasswordTextBox;
                    }
                    else if (ConfirmPasswordTextBox.Password == "") {
                        _tooltip = new ToolTip { Content = "Password cannot be blank" };
                        _tooltip.PlacementTarget = ConfirmPasswordTextBox;
                    }
                    else {
                        _tooltip = new ToolTip { Content = "Passwords do not match" };
                        _tooltip.PlacementTarget = ConfirmPasswordTextBox;
                    }

                    _tooltip.StaysOpen = false;
                    _tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                    _tooltip.HorizontalOffset = 5;
                    _tooltip.VerticalOffset = 5;
                    PasswordTextBox.ToolTip = _tooltip;
                    _tooltip.IsOpen = true;
                }
            }
            else {
                DialogResult = true;
            }           
            
        }

        private void BlinkingCompleted(object sender, System.EventArgs e)
        {
            PasswordTextBox.Style = Resources["errorTemplate"] as Style;
            ConfirmPasswordTextBox.Style = Resources["errorTemplate"] as Style;
        }
    }

    public class PasswordValidation : System.ComponentModel.IDataErrorInfo, System.ComponentModel.INotifyPropertyChanged
    {
        private string firstPassword;
        private string secondPassword;

        public string FirstPassword
        {
            get {
                return firstPassword;
            }
            set {

                if (firstPassword != value) {
                    firstPassword = value;
                    RaisePropertyChanged("SecondPassword");
                }
            }
        }
        public string SecondPassword
        {
            get {
                return secondPassword;
            }
            set {
                if (secondPassword != value) {
                    secondPassword = value;
                    RaisePropertyChanged("FirstPassword");
                }
            }
        }

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get {
                string _result = string.Empty;
                if (columnName == "FirstPassword") {     
                    if (firstPassword == string.Empty) {
                        _result = "Password cannot be blank";
                    }               
                    else if (firstPassword != secondPassword)
                        _result = "Passwords do not match";
                }
                else if (columnName == "SecondPassword") {
                    if (secondPassword == string.Empty) {
                        _result = "Password cannot be blank";
                    }
                    else if (firstPassword != secondPassword)
                        _result = "Passwords Do Not Match";
                }
                return _result;
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public static class PasswordBoxAssistant
    {

        //Taken From http://blog.functionalfun.net/2008/06/wpf-passwordbox-and-data-binding.html

        public static readonly DependencyProperty BoundPassword =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxAssistant), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static readonly DependencyProperty BindPassword = DependencyProperty.RegisterAttached(
            "BindPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false, OnBindPasswordChanged));

        private static readonly DependencyProperty UpdatingPassword =
            DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox box = d as PasswordBox;

            // only handle this event when the property is attached to a PasswordBox
            // and when the BindPassword attached property has been set to true
            if (d == null || !GetBindPassword(d)) {
                return;
            }

            // avoid recursive updating by ignoring the box's changed event
            box.PasswordChanged -= HandlePasswordChanged;

            string newPassword = (string)e.NewValue;

            if (!GetUpdatingPassword(box)) {
                box.Password = newPassword;
            }

            box.PasswordChanged += HandlePasswordChanged;
        }

        private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            // when the BindPassword attached property is set on a PasswordBox,
            // start listening to its PasswordChanged event

            PasswordBox box = dp as PasswordBox;

            if (box == null) {
                return;
            }

            bool wasBound = (bool)(e.OldValue);
            bool needToBind = (bool)(e.NewValue);

            if (wasBound) {
                box.PasswordChanged -= HandlePasswordChanged;
            }

            if (needToBind) {
                box.PasswordChanged += HandlePasswordChanged;
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox box = sender as PasswordBox;

            // set a flag to indicate that we're updating the password
            SetUpdatingPassword(box, true);
            // push the new password into the BoundPassword property
            SetBoundPassword(box, box.Password);
            SetUpdatingPassword(box, false);
        }

        public static void SetBindPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(BindPassword, value);
        }

        public static bool GetBindPassword(DependencyObject dp)
        {
            return (bool)dp.GetValue(BindPassword);
        }

        public static string GetBoundPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(BoundPassword);
        }

        public static void SetBoundPassword(DependencyObject dp, string value)
        {
            dp.SetValue(BoundPassword, value);
        }

        private static bool GetUpdatingPassword(DependencyObject dp)
        {
            return (bool)dp.GetValue(UpdatingPassword);
        }

        private static void SetUpdatingPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(UpdatingPassword, value);
        }
    }


}
