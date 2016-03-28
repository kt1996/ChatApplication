using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatApplication.CustomCommands
{

    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public static class CustomCommands
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand(
                        "Exit",
                        "Exit",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.F4, ModifierKeys.Alt)
                        }
                );

        public static readonly RoutedUICommand ManuallyConnectDialog = new RoutedUICommand(
                        "Connect by IP address",
                        "Connect",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.N, ModifierKeys.Control)
                        }
                );

        public static readonly RoutedUICommand ShowFileTransferWindow = new RoutedUICommand(
                        "Show the File Transfers Window",
                        "ShowFileTransferWindow",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.J, ModifierKeys.Control)
                        }
                );
    }
}
