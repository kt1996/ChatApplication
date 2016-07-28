using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ChatApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            base.OnStartup(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            //Show the exception error
            Exception e = (Exception)ex.ExceptionObject;

            Dialogs.UnhandledExceptionDialog _dg = new Dialogs.UnhandledExceptionDialog(e);
            _dg.Owner = Current.MainWindow;
            _dg.ShowDialog();

            //Exit the app
            Environment.Exit(1);
        }
    }
}
