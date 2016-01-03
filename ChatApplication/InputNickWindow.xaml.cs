using System;
using System.Windows;

namespace ChatApplication
{
    /// <summary>
    /// Interaction logic for InputNickWindow.xaml
    /// </summary>
    public partial class InputNickWindow : Window
    {
        public InputNickWindow()
        {
            InitializeComponent();
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        private void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            ResponseTextBoxKeyUp(ResponseTextBox, new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, System.Windows.Input.Keyboard.PrimaryDevice.ActiveSource, 0, System.Windows.Input.Key.Enter));
            System.Windows.Media.Animation.Storyboard _sb = Resources["BlinkError"] as System.Windows.Media.Animation.Storyboard;
            bool _validDetails = true;
            if (ResponseTextBox.Text == "") {
                _validDetails = false;
                _sb.Begin(BlankErrorLabelText);
                _sb.Begin(((System.Windows.Controls.Primitives.BulletDecorator)BlankErrorLabelText.Parent));
            }
            if (ResponseTextBox.Text.Contains(":") || ResponseTextBox.Text.Contains("<") || ResponseTextBox.Text.Contains(">")) {
                _validDetails = false;
                _sb.Begin(InvalidCharacterErrorLabelText);
                _sb.Begin(((System.Windows.Controls.Primitives.BulletDecorator)InvalidCharacterErrorLabelText.Parent));
            }
            if (_validDetails) {
                DialogResult = true;
            }
        }

        private void WindowContentRendered(object sender, EventArgs e)
        {
            ResponseTextBox.Focus();
        }

        private void ResponseTextBoxKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (ResponseTextBox.Text == "") {
                BlankErrorLabel.Visibility = Visibility.Visible;
            }
            else {
                BlankErrorLabel.Visibility = Visibility.Collapsed;
            }

            if (ResponseTextBox.Text.Contains(":") || ResponseTextBox.Text.Contains("<") || ResponseTextBox.Text.Contains(">")) {
                InvalidCharacterErrorLabel.Visibility = Visibility.Visible;
            }
            else {
                InvalidCharacterErrorLabel.Visibility = Visibility.Collapsed;
            }
            MaxLengthLabel.Visibility = Visibility.Collapsed;
        }
    }
}
