using ChatApplication.DataContainers;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace ChatApplication
{
    public partial class MainWindow : System.Windows.Window
    {
        private void ExitCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (ConnectedPeerDataContainer client in connectedPeersList) {
                Network.NetworkCommunicationManagers.Disconnect(client.socket);
            }
            StopServer();
            isBroadcasting = false;
            System.Windows.Application.Current.Shutdown();
        }

        private void ManuallyConnectCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ManuallyConnectDialog _dialog = new ManuallyConnectDialog();
            _dialog.ShowInTaskbar = false;
            _dialog.Owner = this;
            if (_dialog.ShowDialog() == false) {
                return;
            }
            else {
                string _address = _dialog.IP;
                string _encodedPassword = _dialog.password;
                if (_encodedPassword != null) {
                    byte[] hash = ((System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(_encodedPassword));
                    _encodedPassword = System.BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
                }

                Thread _thread = new Thread(() => ConnectToPeerByIP(_address, _encodedPassword));
                _thread.Name = _address + " handler";
                _thread.IsBackground = true;
                _thread.Start();
            }
        }

        private void ShowFileTransferWindowCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (fileTransferWindow == null) {
                fileTransferWindow = new Dialogs.FileTransferWindow();
                lock (fileTransferWindow) {
                    fileTransferWindow.Closed += (sender2, args) => fileTransferWindow = null;
                    fileTransferWindow.Show();
                }
            }
            else {
                lock (fileTransferWindow) {
                    fileTransferWindow.Activate();
                }
            }
        }
    }
}