using System.Windows;
using System;

namespace ChatApplication.Dialogs
{
    /// <summary>
    /// Interaction logic for UnhandledExceptionDialog.xaml
    /// </summary>
    public partial class UnhandledExceptionDialog : Window
    {
        private Exception exception;

        public UnhandledExceptionDialog(Exception e)
        {
            InitializeComponent();
            exception = e;
            System.Text.StringBuilder _sb = new System.Text.StringBuilder();
            string _hash;
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(System.Reflection.Assembly.GetExecutingAssembly().Location))
                {
                    _hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
            _sb.Append("Application Details-\r\n=============\r\n\r\n-> Version: " + MainWindow.version + "\r\n-> File MD5 Hash: " + _hash + "\r\n\r\nException Message(s)-\r\n==============\r\n\r\n");
            _sb.Append("-> " + e.Message);
            while (e.InnerException != null)
            {
                e = e.InnerException;
                _sb.Append("\r\n-> " + e.Message);
            }
            e = exception;
            OverviewTabTextBox.Text = _sb.ToString();
            _sb.Clear();
            
            _sb.Append("Outermost Exception-\r\n==============================\r\n\r\n-> Message:\r\n" + e.Message + "\r\n-> StackTrace:\r\n" + e.StackTrace);
            int _counter = 1;
            while (e.InnerException != null)
            {
                e = e.InnerException;
                _sb.Append("\r\n\r\n\r\n" + _counter.ToString() + suffix(_counter) + " Inner Exception\r\n==============================\r\n\r\n-> Message:\r\n" + e.Message + "\r\n-> StackTrace:\r\n" + e.StackTrace);
                _counter++;
            }
            DetailsTabTextBox.Text = _sb.ToString();
            LogTabTextBox.Text = ((Application.Current.MainWindow.FindName("Log") as System.Windows.Controls.ListBox).Tag as System.Text.StringBuilder).ToString();
        }

        private void ExitButtonClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveLogToFileButtonClicked(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog _dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            _dialog.IsFolderPicker = true;
            _dialog.Title = "Log File Location";
            _dialog.EnsurePathExists = true;
            _dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (_dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                string _folder = _dialog.FileName;

                int _tries = 0;
                bool _logSuccessfullyWritten = false;

                string _hash;
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = System.IO.File.OpenRead(System.Reflection.Assembly.GetExecutingAssembly().Location))
                    {
                        _hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    }
                }

                string _data = "Please send this log with a brief description of what caused this error/ what you were doing to kaustubht96<at>gmail<dot>com\r\n\r\n" + "Application Details-\r\n============================\r\n\r\n-> Version: " + MainWindow.version + "\r\n-> File MD5 Hash: " + _hash + "\r\n\r\n" + DetailsTabTextBox.Text + "\r\n\r\n" + "Application Log-\r\n============================\r\n\r\n" + LogTabTextBox.Text;

                Exception ex = exception;

                while (_tries < 5 & !_logSuccessfullyWritten)
                {
                    try
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(_folder + "\\ChatApp_Error_Log_" + DateTime.Now.ToLongTimeString().Replace(':','-') + "_" + DateTime.Now.ToShortDateString() + ".txt" ))
                        {
                            file.WriteLine(_data);
                        }
                        _logSuccessfullyWritten = true;
                    }
                    catch
                    {
                        _tries++;
                    }
                }

                if (!_logSuccessfullyWritten)
                {
                    MessageBox.Show("Failed To Write Log to File");
                }else
                {
                    MessageBox.Show("Log Successfully Written to File");
                }
            }
        }

        private string suffix(int num)
        {
            if(num%10 == 1)
            {
                return "st";
            }
            if(num%10 == 2)
            {
                return "nd";
            }
            if(num%10 == 3)
            {
                return "rd";
            }
            else
            {
                return "th";
            }
        }
    }
}
