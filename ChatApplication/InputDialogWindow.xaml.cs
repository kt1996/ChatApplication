using System;
using System.Windows;
using System.Windows.Controls;

namespace ChatApplication
{
    public partial class InputDialogWindow : Window
    {
        //buttons_needed is 1 if all buttons are needed, and 0 if only ok button is needed
        public InputDialogWindow(string title, string description = "", string defaultText = "", int buttonsNeeded = 1, int textLimit = 0)
        {
            InitializeComponent();
            dialog.Title = title;
            ResponseTextBox.Text = defaultText;
            if (description == "")
            {
                mainGrid.Children.RemoveAt(0);
                ResponseTextBox.SetValue(Grid.RowProperty, 0);
                responsePanel.SetValue(Grid.RowProperty, 1);
            }
            else
            {
                Description.Content = description;
            }
            if (buttonsNeeded == 0)
            {
                responsePanel.Children.RemoveAt(1);
                ((Button)responsePanel.Children[0]).Margin = new Thickness(0);
            }
            if (textLimit != 0)
            {
                ResponseTextBox.MaxLength = textLimit;
            }
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        private void OKButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void WindowContentRendered(object sender, EventArgs e)
        {
            ResponseTextBox.SelectAll();
            ResponseTextBox.Focus();
        }

    }
}
