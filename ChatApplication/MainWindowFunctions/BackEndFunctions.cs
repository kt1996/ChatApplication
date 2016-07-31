using ChatApplication.DataContainers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ChatApplication
{
    public partial class MainWindow : System.Windows.Window
    {
        private void ConnectToPeerByIP(string address, string password = null)
        {
            //Invalid IP address
            IPAddress _ip;
            if (!IPAddress.TryParse(address, out _ip)) {
                System.Windows.MessageBox.Show("Invalid IP address");
                WriteToLogbox("Invalid IP address entered \"" + address + "\"");
                return;
            }

#if !DEBUG
            //Trying to connect to the device itself
            if (IPAddress.IsLoopback(IPAddress.Parse(address)))
            {
                MessageBox.Show("Don't try to talk to yourself!!");
                WriteToLogbox("Feeling Lonely");
                return;
            }

#endif

            Socket _peerSocket;
            SocketException _exception;
            if (Network.NetworkCommunicationManagers.ConnectToEndPoint(address, out _peerSocket, out _exception)) {
                try {
                    if (Greet(false, _peerSocket, password)) {
                        AcceptGreetAndProcess(false, _peerSocket);
                        return;
                    }
                }
                catch (Exception) {
                    //Connection problem while greeting
                }
                return;
            }
            else {
                if (_exception != null) {
                    WriteToLogbox("Exception caught- " + _exception.Message);
                    if (_exception.ErrorCode == 10051) {
                        MessageBox.Show("Unreachable address: \"" + address + "\"");
                    }
                }
                else {
                    MessageBox.Show("Failed To establish connection to \"" + address + "\"");
                    WriteToLogbox(string.Format("Failed To establish connection to client: {0}", address));
                }
                return;
            }
        }

        public void Disconnect(string nickAndIP)
        {
            ConnectedPeerDataContainer _obj = new ConnectedPeerDataContainer();
            _obj.nick = "";
            _obj.socket = null;
            string _peerSocketRemoteEndPointString;
            foreach (ConnectedPeerDataContainer _peer in connectedPeersList) {
                _peerSocketRemoteEndPointString = _peer.socket.RemoteEndPoint.ToString();
                if ((_peer.nick + ":" + _peerSocketRemoteEndPointString.Remove(_peerSocketRemoteEndPointString.LastIndexOf(':'))) == nickAndIP) {
                    _obj = _peer;
                    break;
                }
            }
            if (_obj.socket != null) {
                Network.NetworkCommunicationManagers.Disconnect(_obj.socket);
            }

            Console.WriteLine("Client " + _obj.nick + "(" + nickAndIP.Remove(0, nickAndIP.IndexOf(':') + 1) + ") has been successfully disconnected");
            connectedPeersList.Remove(_obj);
        }

        private void EndAcceptConnection(IAsyncResult ar)
        {
            if (isServerRunning && serverSocket != null) {
                try {
                    Socket _clientSocket = serverSocket.EndAccept(ar);

                    //send the standard greeting to the server
                    try {
                        if (!Greet(true, _clientSocket)) {
                            serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);
                            return;
                        }
                    }
                    catch (Exception) {
                        serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);
                        return;
                        //Error while greeting
                    }
                    

                    Thread thread = new Thread(() => AcceptGreetAndProcess(true, _clientSocket));
                    string _clientSocketRemoteEndPointString = _clientSocket.RemoteEndPoint.ToString();
                    thread.Name = _clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.IndexOf(':')) + " handler";
                    thread.IsBackground = true;
                    thread.Start();

                    serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);
                }
                catch (Exception e) {
                    WriteToLogbox("Exception caught- " + e.Source);
                    return;
                }
            }
        }

        private void EndReceiveBroadcasts(IAsyncResult ar)
        {
            IPEndPoint _ip = new IPEndPoint(IPAddress.Any, 15069);
            byte[] _bytes = broadcastReceiver.EndReceive(ar, ref _ip);
            string _message = Encoding.ASCII.GetString(_bytes);
            if (!_message.StartsWith("Hello everybody- ")) {
                broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
                return;
            }
            _message = _message.Remove(0, "Hello everybody- ".Length);
            string[] _messageParts = (_message).Split(new char[] { ':' });
            if (_messageParts.Length != 2 || (_messageParts[0] == nick && _messageParts[1] == encodedMachineName)) {
                broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
                return;
            }
            AddPeer(new PeerDataContainer() { IP = _ip.ToString().Remove(_ip.ToString().IndexOf(':')), nick = _messageParts[0], time = DateTime.Now, encodedMachineName = _messageParts[1] });

            broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
        }

        private bool Greet(bool serverOrClient, Socket socket, string password = null)
        {
            //Sends the initial greeting message with the nick, serverOrClient
            //is true for server and false in case of client

            string _message;

            if (serverOrClient) {
                _message = "Hello client, this is " + nick + ":" + encodedMachineName;
            }
            else {
                _message = "Hello server, this is " + nick + ":" + encodedMachineName;
                if (password != null && password != "d41d8cd98f00b204e9800998ecf8427e") {
                    _message = _message + ":" + password;
                }
            }

            if (!Network.NetworkCommunicationManagers.SendIntOverSocket(socket, _message.Length)) {
                // connection was broken
                return false;
            }

            if (!Network.NetworkCommunicationManagers.SendStringOverSocket(socket, _message)) {
                // connection was broken
                return false;
            }

            return true;
        }

        private void AcceptGreetAndProcess(bool serverOrClient, Socket socket)
        {
            // Checks greeting message and processes if correct, 
            // Extracts the nick and machine_name if true
            // serverOrClient is true for server and false in case of client

            //server recieves hello server, client receives hello client
            string _prefix;
            int _size;

            if (serverOrClient) {
                _prefix = "Hello server, this is ";
            }
            else {
                _prefix = "Hello client, this is ";
            }

            string _message;

            if (!Network.NetworkCommunicationManagers.ReceiveIntOverSocket(socket, out _size)) {
                Network.NetworkCommunicationManagers.Disconnect(socket);
                return;
            }

            if (!Network.NetworkCommunicationManagers.ReceiveStringOverSocket(socket, out _message, _size)) {
                Network.NetworkCommunicationManagers.Disconnect(socket);
                return;
            }

            //didn't start with the given greeting
            if (_message.Substring(0, _prefix.Length) != _prefix) {
                Network.NetworkCommunicationManagers.Disconnect(socket);
                return;
            }

            //no nickname sent
            _message = _message.Remove(0, _prefix.Length);
            if (_message.Length == 0) {
                Network.NetworkCommunicationManagers.Disconnect(socket);
                return;
            }

            string _clientSocketRemoteEndPointString;
            string _socketRemoteEndPointString = socket.RemoteEndPoint.ToString();

            //Check if already connected to the client
            foreach (ConnectedPeerDataContainer _client in connectedPeersList) {
                try {
                    _clientSocketRemoteEndPointString = _client.socket.RemoteEndPoint.ToString();
                    if (_clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.LastIndexOf(':')) == _socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':'))) {
                        WriteToLogbox("Already Connected to Client " + _socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':')));
                        MessageBox.Show("Already Connected to Client");
                        Network.NetworkCommunicationManagers.Disconnect(socket);
                        return;
                    }
                }
                catch (ObjectDisposedException) { }
            }

            ConnectedPeerDataContainer obj = new ConnectedPeerDataContainer();
            int _indexOfDelimiter = _message.IndexOf(':');
            if (_indexOfDelimiter == -1) {
                Network.NetworkCommunicationManagers.Disconnect(socket);
                return;
            }
            obj.nick = _message.Substring(0, _indexOfDelimiter);
            string _remainingMessage = _message.Substring(_indexOfDelimiter + 1);

            int _temp;

            if (serverOrClient) {
                if (password != null) {
                    _indexOfDelimiter = _remainingMessage.IndexOf(':');

                    if (_indexOfDelimiter == -1) {
                        Network.NetworkCommunicationManagers.SendIntOverSocket(socket, (int)PrimaryCommands.Error);
                        Network.NetworkCommunicationManagers.SendIntOverSocket(socket, (int)ErrorCommands.PasswordRequired);
                        Network.NetworkCommunicationManagers.ReceiveIntOverSocket(socket, out _temp);
                        Network.NetworkCommunicationManagers.Disconnect(socket);
                        return;
                    }

                    string _providedPassword = _remainingMessage.Substring(_indexOfDelimiter + 1);

                    _remainingMessage = _remainingMessage.Substring(0, _indexOfDelimiter);

                    if (_providedPassword != password) {
                        Network.NetworkCommunicationManagers.SendIntOverSocket(socket, (int)PrimaryCommands.Error);
                        Network.NetworkCommunicationManagers.SendIntOverSocket(socket, (int)ErrorCommands.PasswordIncorrect);
                        Network.NetworkCommunicationManagers.ReceiveIntOverSocket(socket, out _temp);
                        Network.NetworkCommunicationManagers.Disconnect(socket);
                        return;
                    }
                }
            }

            obj.encodedMachineName = _remainingMessage;
            obj.socket = socket;

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { AddNewTab(obj.nick, _socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':'))); }));
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_socketRemoteEndPointString.Remove(_socketRemoteEndPointString.LastIndexOf(':')), "Connected", obj.nick, 0); }));

            if (serverOrClient) {
                if (!Network.NetworkCommunicationManagers.SendIntOverSocket(socket, (int)PrimaryCommands.OK)) {
                    Network.NetworkCommunicationManagers.Disconnect(socket);
                    return;
                };
                if (!Network.NetworkCommunicationManagers.ReceiveIntOverSocket(socket, out _temp)) {
                    Network.NetworkCommunicationManagers.Disconnect(socket);
                    return;
                };
            }
            else {
                if (!Network.NetworkCommunicationManagers.ReceiveIntOverSocket(socket, out _temp)) {
                    Network.NetworkCommunicationManagers.Disconnect(socket);
                    return;
                };
                if (!Network.NetworkCommunicationManagers.SendIntOverSocket(socket, (int)PrimaryCommands.OK)) {
                    Network.NetworkCommunicationManagers.Disconnect(socket);
                    return;
                };                
            }

            _clientSocketRemoteEndPointString = socket.RemoteEndPoint.ToString();
            string _ip = _clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.LastIndexOf(':'));
            string _nick = obj.nick;
            try {
                switch ((PrimaryCommands)_temp) {
                    case PrimaryCommands.OK:
                        obj.key = Encryption.PerformAssymetricKeyExchangeUsingECDiffieHellmanOnSocket(socket);
                        break;
                    case PrimaryCommands.Error:
                        Network.NetworkCommunicationManagers.ReceiveIntOverSocket(socket, out _temp);
                        Network.NetworkCommunicationManagers.SendIntOverSocket(socket, (int)PrimaryCommands.OK);
                        switch ((ErrorCommands)_temp) {
                            case ErrorCommands.PasswordRequired:
                                WriteToLogbox("Password Required for " + _ip);
                                WriteToLogbox("Disconnected- " + _ip);
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Password Required", nick, 0); }));
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Disconnected", nick, 0); }));
                                try {
                                    socket.Shutdown(SocketShutdown.Both);
                                    socket.Close();
                                }
                                catch (Exception) { }
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                                    ManuallyConnectDialog _dialog;
                                    while (true) {
                                        _dialog = new ManuallyConnectDialog(_ip, _nick, "Password Required");
                                        _dialog.ShowInTaskbar = false;
                                        _dialog.Owner = this;
                                        if (_dialog.ShowDialog() == false) {
                                            return;
                                        }
                                        else {
                                            string _address = _dialog.IP;

                                            if (_dialog.password == null) {
                                                continue;
                                            }

                                            byte[] hash = ((System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(_dialog.password));
                                            string _encodedPassword = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

                                            Thread _thread = new Thread(() => ConnectToPeerByIP(_address, _encodedPassword));
                                            _thread.Name = _address + " handler";
                                            _thread.IsBackground = true;
                                            _thread.Start();
                                            break;
                                        }
                                    }
                                }));
                                return;
                            case ErrorCommands.PasswordIncorrect:
                                WriteToLogbox("Incorrect password provided for " + _ip);
                                WriteToLogbox("Disconnected- " + _ip);
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Incorrect Password", nick, 0); }));
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Disconnected", nick, 0); }));
                                try {
                                    socket.Shutdown(SocketShutdown.Both);
                                    socket.Close();
                                }
                                catch (Exception) { }
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                                    ManuallyConnectDialog _dialog;
                                    while (true) {
                                        _dialog = new ManuallyConnectDialog(_ip, _nick, "Incorrect Password");
                                        _dialog.ShowInTaskbar = false;
                                        _dialog.Owner = this;
                                        if (_dialog.ShowDialog() == false) {
                                            return;
                                        }
                                        else {
                                            if (_dialog.password == null) {
                                                continue;
                                            }
                                            string _address = _dialog.IP;

                                            byte[] hash = ((System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(_dialog.password));
                                            string _encodedPassword = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

                                            Thread _thread = new Thread(() => ConnectToPeerByIP(_address, _encodedPassword));
                                            _thread.Name = _address + " handler";
                                            _thread.IsBackground = true;
                                            _thread.Start();
                                            break;
                                        }
                                    }
                                }));
                                return;
                        }
                        return;
                    default:
                        _message = "Invalid MessageCode Received, The other client is most probably running a newer version of the application with a new Feature.. !!";
                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, _message, _nick, 0); }));
                        WriteToLogbox("Invalid MessageCode- " + _nick + " (" + _clientSocketRemoteEndPointString + ") : " + _temp);
                        break;
                }
            }
            catch (Exception) {
            }
            if(obj.key == null) {
                Network.NetworkCommunicationManagers.Disconnect(socket);
                return;
            }
            connectedPeersList.Add(obj);
            ProcessClient(obj);
        }

        private void ProcessClient(ConnectedPeerDataContainer client)
        {
            int _messageType = 0;
            PrimaryCommands _primaryCommand;
            string _clientSocketRemoteEndPointString = client.socket.RemoteEndPoint.ToString();
            string _ip = _clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.LastIndexOf(':'));
            string _nick = client.nick;
            Socket _peerSocket = client.socket;
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { AddNewTab(client.nick, _clientSocketRemoteEndPointString.Remove(_clientSocketRemoteEndPointString.LastIndexOf(':'))); }));
            WriteToLogbox("Connected to- " + _ip);
            try {
                string _message;
                int _size;
                while (_peerSocket.Connected) {

                    if (!Network.NetworkCommunicationManagers.ReceiveDecodedIntOverSocket(_peerSocket, client.key, out _messageType)) {
                        break;
                    }
                    _primaryCommand = (PrimaryCommands)_messageType;

                    switch (_primaryCommand) {
                        case PrimaryCommands.TextMessage:
                            if (!Network.NetworkCommunicationManagers.ReceiveDecodedIntOverSocket(_peerSocket, client.key, out _size)) {
                                break;
                            }

                            if (!Network.NetworkCommunicationManagers.ReceiveDecodedStringOverSocket(_peerSocket, client.key, out _message, _size)) {
                                break;
                            }

                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, _message, _nick, 1); }));
                            WriteToLogbox("Message Received- " + _nick + " (" + _clientSocketRemoteEndPointString + ") : " + _message);
                            break;

                        case PrimaryCommands.FileTransfer:
                            int _port1;
                            Network.NetworkCommunicationManagers.ReceiveIntOverSocket(_peerSocket, out _port1);
                            FileTransferContainer fileTransferContainer = new FileTransferContainer() {
                                status = FileTransferStatus.Running,
                                fileName = "(Fetching....)",
                                ID = _nick + " (" + _ip + ")",
                                progress = 0,
                                size = "(Fetching....)",
                                transferType = FileTransferType.Download,
                                pausedBy = PausedBy.None,
                            };

                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                                lock (RunningTransfers) {
                                    RunningTransfers.Add(fileTransferContainer);
                                }
                            }));

                            Thread _thread = new Thread(() => {
                                if (!(new Network.FileTransfer(fileTransferContainer)).AcceptFileTransfer(_peerSocket, _port1, this)) {
                                    WriteToLogbox("File Transfer from " + _nick + " (" + _clientSocketRemoteEndPointString + ") failed");
                                    lock (fileTransferContainer) {
                                        if(fileTransferContainer.status != FileTransferStatus.Cancelled) {
                                            fileTransferContainer.status = FileTransferStatus.Error;
                                        }
                                    }
                                }
                                else {
                                    WriteToLogbox("File Transfer from " + _nick + " (" + _clientSocketRemoteEndPointString + ") done");
                                    lock (fileTransferContainer) {
                                        fileTransferContainer.status = FileTransferStatus.Finished;
                                    }
                                }
                            });
                            _thread.Name = "File Transfer from " + _clientSocketRemoteEndPointString;
                            _thread.IsBackground = true;
                            _thread.Start();
                            break;

                        default:
                            _message = "Invalid MessageCode Received. The other client is most probably running a newer version of the application with a new Feature.. !!";
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, _message, _nick, 0); }));
                            WriteToLogbox("Unsupported MessageCode from- " + _nick + " (" + _clientSocketRemoteEndPointString + ") received");
                            Network.NetworkCommunicationManagers.Disconnect(_peerSocket);
                            new Thread(() => ConnectToPeerByIP(_ip)) {
                                Name = _ip + " handler", IsBackground = true
                            }.Start();
                            try
                            {
                                connectedPeersList.Remove(client);
                            }
                            catch (Exception) { }
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Attempting to reconnect", _nick, 0); }));
                            WriteToLogbox("Attempting to reconnect- " + _ip);
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Disconnected", _nick, 0); }));
                            WriteToLogbox("Disconnected- " + _ip);
                            return;
                    }
                }
                connectedPeersList.Remove(client);
            }
            catch (Exception) {
                try {
                    connectedPeersList.Remove(client);
                }
                catch (Exception) { }
            }
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { WriteToTab(_ip, "Disconnected", _nick, 0); }));
            WriteToLogbox("Disconnected- " + _ip);
        }

        private void ReceiveBroadcasts()
        {
            try {
                broadcastReceiver = new UdpClient(15069);
                broadcastReceiver.BeginReceive(EndReceiveBroadcasts, null);
            }
            catch (Exception) {
                MessageBox.Show("   Failed to start broadcast receiver\nsome other app listening on port 15069");
                WriteToLogbox("Failed to receive broadcasts");
            }
        }

        private void SendMessage(string msg, ConnectedPeerDataContainer client)
        {
            Socket _peerSocket = client.socket;

            if (!Network.NetworkCommunicationManagers.SendEncryptedIntOverSocket(_peerSocket, client.key, (int)PrimaryCommands.TextMessage)) {
                // connection was broken
                return;
            }


            if (!Network.NetworkCommunicationManagers.SendEncryptedIntOverSocket(_peerSocket, client.key, msg.Length)) {
                // connection was broken
                return;
            }

            if (!Network.NetworkCommunicationManagers.SendEncryptedStringOverSocket(_peerSocket, client.key, msg)) {
                // connection was broken
                return;
            }

            WriteToLogbox("Message Sent to " + client.nick + " (" + _peerSocket.RemoteEndPoint.ToString() + "): " + msg);

        }

        private void Broadcast(object state)
        {
            isBroadcasting = true;

            if (numberOfBroadcastsSinceListUpdate % 4 == 0) {
                numberOfBroadcastsSinceListUpdate = 0;
                Network.NetworkCommunicationManagers.UpdateBroadcastList(out broadcastIPs);
            }

            Network.NetworkCommunicationManagers.BroadcastFromList("Hello everybody- " + nick + ":" + encodedMachineName, broadcastIPs);

            numberOfBroadcastsSinceListUpdate++;
            broadcastTimer.Change(10000, Timeout.Infinite);
        }

        private void StopBroadcasts()
        {
            WriteToLogbox("Stopping Status Broadcasts");
            isBroadcasting = false;
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { ToggleBroadcast.IsChecked = false; }));
        }

        private void StartServer()
        {
            short _pos = Network.NetworkCommunicationManagers.TryBindServer(out serverSocket);
            if (_pos == -1) {
                WriteToLogbox("Failed to start server all ports taken\n");
                isServerRunning = false;
                return;
            }

            serverSocket.Listen(5);
            serverSocket.BeginAccept(new AsyncCallback(EndAcceptConnection), null);
            isServerRunning = true;
            WriteToLogbox("Server is up and running on port " + TCPPorts[_pos]);
        }

        private void StopServer()
        {
            if (serverSocket != null) {
                isServerRunning = false;
                serverSocket.Close();
                WriteToLogbox("Server Shutting Down");
                return;
            }
        }
    }
}