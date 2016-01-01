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
            string message = "     **Unhandled Exception**\nMessage: " + e.Message + "\nStackTrace: " + e.StackTrace;

            while (e.InnerException != null)
            {
                e = e.InnerException;
                message += "\n\nInner Exception-\nMessage: " + e.Message + "\nStackTrace: " + e.StackTrace;
            }
            MessageBox.Show(message);

            //Exit the app
            Environment.Exit(1);
        }
    }
}
