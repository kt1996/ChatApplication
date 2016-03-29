using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Windows.Threading;

namespace ChatApplication.Network
{
    public class FileTransfer
    {
        private long acknowledgedTransfers;
        private object syncObject;
        int port1, port2;
        public Socket controlSocket, dataSocket;
        public DataContainers.FileTransferContainer fileTransferContainer;
        public static MainWindow mainWindow;
        public AutoResetEvent resetEvent;
        public static System.ComponentModel.BindingList<DataContainers.FileTransferContainer> RunningTransfers;
        private const int FILE_BUFFER_SIZE = 8175;

        public FileTransfer(DataContainers.FileTransferContainer fileTransferContainer, string filePath = null, int intialPort = 0)
        {
            resetEvent = new AutoResetEvent(false);
            acknowledgedTransfers = 0;
            syncObject = new object();
            controlSocket = null;
            dataSocket = null;

            if (fileTransferContainer.transferType == FileTransferType.Upload) {
                port1 = intialPort;

                dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                dataSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port1));
                dataSocket.Listen(1);
                dataSocket.BeginAccept(new AsyncCallback(EndAcceptDataConnection), filePath);
            }

            this.fileTransferContainer = fileTransferContainer;
            fileTransferContainer.FileTransferClassInstance = this;
        }        

        static internal byte[] packFileMetadata(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            long length;
            using (FileStream _fs = new FileStream(filePath, FileMode.Open)) {
                length = _fs.Length;
            }                
            byte[] arr1 = System.Text.Encoding.UTF8.GetBytes(fileName);
            byte[] arr2 = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(length));
            int x = arr2.Length;
            byte[] arr = new byte[arr1.Length + arr2.Length];
            Buffer.BlockCopy(arr1, 0, arr, 0, arr1.Length);
            Buffer.BlockCopy(arr2, 0, arr, arr1.Length, arr2.Length);
            return arr;
        }

        static internal void unpackFileMetadata(byte[] buffer, out string filename, out long size)
        {
            byte[] _temp = new byte[8];
            Buffer.BlockCopy(buffer, buffer.Length - 8, _temp, 0, 8);
            size = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt64(_temp, 0));
            _temp = new byte[buffer.Length - 8];
            Buffer.BlockCopy(buffer, 0, _temp, 0, _temp.Length);
            filename = System.Text.Encoding.UTF8.GetString(_temp);
        }

        internal void managedSendFileOverSockets(string filePath)
        {
            short _count = 0;
            const short _INITIAL = 5000;

            Thread _thread = new Thread(() => monitorControlChannel());
            _thread.Name = "Control Socket monitor for " + dataSocket.RemoteEndPoint.ToString();
            _thread.IsBackground = true;
            _thread.Start();

            using (FileStream _fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                byte[] _fileBuffer = new byte[FILE_BUFFER_SIZE];

                long _size = _fs.Length;

                try {
                    long _index = 0;

                    //Send the initial chunk of data
                    while (dataSocket.Connected && controlSocket.Connected && _index < _size) {

                        if(fileTransferContainer.status == FileTransferStatus.Paused) {
                            resetEvent.WaitOne();
                        }

                        _fs.Position = _index;
                        int _read = _fs.Read(_fileBuffer, 0, _fileBuffer.Length);

                        try {
                            NetworkCommunicationManagers.SendIntOverSocket(dataSocket, _read);

                            if (_read != _fileBuffer.Length) {
                                byte[] _temp = new byte[_read];
                                Buffer.BlockCopy(_fileBuffer, 0, _temp, 0, _read);
                                NetworkCommunicationManagers.SendByteArrayOverSocket(dataSocket, _temp);
                            }
                            else {
                                NetworkCommunicationManagers.SendByteArrayOverSocket(dataSocket, _fileBuffer);
                            }
                        }
                        catch (SocketException) {
                            continue;
                        }

                        _index += _read;
                        _count++;
                        if (_count % 200 == 0) {
                            lock (RunningTransfers) {
                                if (Math.Round(((((double)_index / _size) * 100) - fileTransferContainer.progress), 1) >= 0.1) {
                                    fileTransferContainer.progress = (float)Math.Round(((double)_index / _size) * 100, 1);
                                }
                            }
                        }
                        if (_count == _INITIAL) {
                            _count = 0;
                            break;
                        }
                    }

                    //Send the remaining data
                    while (dataSocket.Connected && controlSocket.Connected && _index < _size) {

                        _fs.Position = _index;
                        int _read = _fs.Read(_fileBuffer, 0, _fileBuffer.Length);

                        try {
                            NetworkCommunicationManagers.SendIntOverSocket(dataSocket, _read);
                            if (_read != _fileBuffer.Length) {
                                byte[] _temp = new byte[_read];
                                Buffer.BlockCopy(_fileBuffer, 0, _temp, 0, _read);
                                NetworkCommunicationManagers.SendByteArrayOverSocket(dataSocket, _temp);
                            }
                            else {
                                NetworkCommunicationManagers.SendByteArrayOverSocket(dataSocket, _fileBuffer);
                            }
                        }
                        catch (SocketException) {
                            continue;
                        }

                        _index += _read;
                        _count++;
                        if (_count % 200 == 0) {
                            lock(RunningTransfers){
                                if (Math.Round(((((double)_index / _size) * 100) - fileTransferContainer.progress), 1) >= 0.1) {
                                    fileTransferContainer.progress = (float)Math.Round((((double)_index/_size) * 100), 1);
                                }
                            }                            
                            bool _wait = true;
                            while (true) {
                                lock (syncObject) {
                                    if (acknowledgedTransfers > 0) {
                                        acknowledgedTransfers--;
                                        _wait = false;
                                        break;
                                    }
                                }
                                if (_wait) {
                                    Thread.Sleep(100);
                                }
                            }
                        }

                    }
                    if(_index == _size) {
                        lock (RunningTransfers) {
                            fileTransferContainer.progress = 100;
                            fileTransferContainer.status = FileTransferStatus.Finished;
                        }
                    }
                    else {
                        lock (RunningTransfers) {
                            if(fileTransferContainer.status != FileTransferStatus.Cancelled) {
                                fileTransferContainer.status = FileTransferStatus.Error;
                            }                            
                        }
                    }                    
                }
                catch {
                    lock (RunningTransfers) {
                        if (fileTransferContainer.status != FileTransferStatus.Cancelled) {
                            fileTransferContainer.status = FileTransferStatus.Error;
                        }
                    }
                }
            }
        }

        internal void monitorControlChannel()
        {
            try {
                byte[] arr;
                FileTransferCommands _command;
                while (controlSocket.Connected) {
                    if(!NetworkCommunicationManagers.ReceiveByteArrayOverSocket(controlSocket, out arr, 1)) {
                        continue;
                    }
                    _command = (FileTransferCommands)arr[0];
                    switch (_command) {

                        case FileTransferCommands.BlockTransferred:
                            lock (syncObject) {
                                acknowledgedTransfers++;
                            }
                            break;

                        case FileTransferCommands.EndTransfer:
                            NetworkCommunicationManagers.Disconnect(controlSocket);
                            NetworkCommunicationManagers.Disconnect(dataSocket);
                            return;

                        case FileTransferCommands.Pause:
                            lock (RunningTransfers) {
                                fileTransferContainer.status = FileTransferStatus.Paused;

                                if(fileTransferContainer.pausedBy == PausedBy.None) {
                                    fileTransferContainer.pausedBy = PausedBy.OtherPeer;
                                }else if(fileTransferContainer.pausedBy == PausedBy.User) {
                                    fileTransferContainer.pausedBy = PausedBy.Both;
                                }
                            }

                            ThreadPool.QueueUserWorkItem(state => {
                                lock (controlSocket) {
                                    NetworkCommunicationManagers.SendByteArrayOverSocket(controlSocket, new byte[] { (byte)FileTransferCommands.PauseOrResumeRequestReceived });
                                }
                            });

                            break;

                        case FileTransferCommands.PauseOrResumeRequestReceived:
                            lock (RunningTransfers) {
                                if (fileTransferContainer.transferType == FileTransferType.Upload) {
                                    if (fileTransferContainer.status == FileTransferStatus.Paused && fileTransferContainer.pausedBy == PausedBy.None) {
                                        fileTransferContainer.status = FileTransferStatus.Running;
                                        resetEvent.Set();
                                    }
                                }else if(fileTransferContainer.transferType == FileTransferType.Download) {
                                    if (fileTransferContainer.pausedBy != PausedBy.None && fileTransferContainer.status == FileTransferStatus.Running) {       
                                        fileTransferContainer.status = FileTransferStatus.Paused;
                                    }
                                }                                                               
                            }
                            break;

                        case FileTransferCommands.Resume:
                            lock (RunningTransfers) {
                                if(fileTransferContainer.pausedBy == PausedBy.OtherPeer) {
                                    fileTransferContainer.pausedBy = PausedBy.None;
                                    fileTransferContainer.status = FileTransferStatus.Running;
                                    resetEvent.Set();
                                }
                                else if(fileTransferContainer.pausedBy == PausedBy.Both) {
                                    fileTransferContainer.pausedBy = PausedBy.User;
                                }
                                
                                ThreadPool.QueueUserWorkItem(state => {
                                    lock (controlSocket) {
                                        NetworkCommunicationManagers.SendByteArrayOverSocket(controlSocket, new byte[] { (byte)FileTransferCommands.PauseOrResumeRequestReceived });
                                    }
                                });
                            }
                            break;
                    }
                }
            }
            catch (ObjectDisposedException) {}
            catch (SocketException) {}
            finally {
                NetworkCommunicationManagers.Disconnect(controlSocket);
                NetworkCommunicationManagers.Disconnect(dataSocket);
            }
        }

        internal bool managedReceiveFileOverSockets(string filePath, long length)
        {
            short _count = 0;
            long _index = 0;
            int _size = 1;

            Thread _thread = new Thread(() => monitorControlChannel());
            _thread.Name = "Control Socket monitor for " + dataSocket.RemoteEndPoint.ToString();
            _thread.IsBackground = true;
            _thread.Start();

            try {
                using (FileStream _fs = new FileStream(filePath + ".temp", FileMode.CreateNew)) {
                    _fs.SetLength(length);
                    byte[] _buffer;

                    while (dataSocket.Connected && controlSocket.Connected && _index < length) {

                        if (fileTransferContainer.status == FileTransferStatus.Paused) {
                            resetEvent.WaitOne();
                        }
                        if (!NetworkCommunicationManagers.ReceiveIntOverSocket(dataSocket, out _size)) {
                            continue;
                        };
                        _buffer = new byte[_size];
                        if (!NetworkCommunicationManagers.ReceiveByteArrayOverSocket(dataSocket, out _buffer, _size)) {
                            continue;
                        };


                        _fs.Position = _index;
                        _fs.Write(_buffer, 0, _buffer.Length);
                        _index += _size;
                        _count++;
                        if (_count % 200 == 0) {
                            lock (RunningTransfers) {
                                if (Math.Round(((((double)_index / length) * 100) - fileTransferContainer.progress), 1) >= 0.1) {
                                    fileTransferContainer.progress = (float)Math.Round((((double)_index / length) * 100), 1);
                                }
                            }
                            _count = 0;
                            ThreadPool.QueueUserWorkItem(state => {
                                lock (controlSocket) {
                                    NetworkCommunicationManagers.SendByteArrayOverSocket(controlSocket, new byte[] { (byte)FileTransferCommands.BlockTransferred });
                                }
                            });
                        }
                    }
                }
                lock (controlSocket) {
                    NetworkCommunicationManagers.SendByteArrayOverSocket(controlSocket, new byte[] { (byte)FileTransferCommands.EndTransfer });
                }
            }
            catch (ObjectDisposedException) {
                NetworkCommunicationManagers.Disconnect(controlSocket);
                NetworkCommunicationManagers.Disconnect(dataSocket);
            }
            catch (SocketException) {
                NetworkCommunicationManagers.Disconnect(controlSocket);
                NetworkCommunicationManagers.Disconnect(dataSocket);
            }

            if (_index < length) {
                try {
                    File.Delete(filePath + ".temp");
                }
                catch { }                
                lock (RunningTransfers) {
                    if (fileTransferContainer.status != FileTransferStatus.Cancelled) {
                        fileTransferContainer.status = FileTransferStatus.Error;
                    }
                }
                return false;
            }
            else {
                try {
                    File.Move(filePath + ".temp", filePath);
                }
                catch (IOException) {
                    try {
                        File.Delete(filePath + ".temp");
                    }
                    catch { }
                }
                lock (RunningTransfers) {
                    fileTransferContainer.status = FileTransferStatus.Finished;
                    fileTransferContainer.progress = 100;
                }
            }
            return true;
        }

        internal void EndAcceptDataConnection(IAsyncResult ar)
        {
            try {
                port2 = NetworkCommunicationManagers.FindNextFreeTcpPort();
                Socket _clientSocket = dataSocket.EndAccept(ar);
                NetworkCommunicationManagers.Disconnect(dataSocket);
                dataSocket = _clientSocket;
                NetworkCommunicationManagers.SendIntOverSocket(_clientSocket, port2);

                controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                controlSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port2));
                controlSocket.Listen(1);
                controlSocket.BeginAccept(new AsyncCallback(EndAcceptControlConnection), ar.AsyncState);               
            }
            catch (Exception) {
                NetworkCommunicationManagers.Disconnect(dataSocket);
                lock (RunningTransfers) {
                    if(fileTransferContainer.status != FileTransferStatus.Cancelled) {
                        fileTransferContainer.status = FileTransferStatus.Error;
                    }
                }
                return;
            }
        }

        internal void EndAcceptControlConnection(IAsyncResult ar)
        {
            try {
                string _filePath = (string)ar.AsyncState;
                Socket _clientSocket = controlSocket.EndAccept(ar);
                NetworkCommunicationManagers.Disconnect(controlSocket);
                controlSocket = _clientSocket;
                byte[] _buffer = packFileMetadata(_filePath);

                NetworkCommunicationManagers.SendIntOverSocket(dataSocket, _buffer.Length);
                NetworkCommunicationManagers.SendByteArrayOverSocket(dataSocket, _buffer);

                fileTransferContainer.FileTransferClassInstance = this;
                managedSendFileOverSockets(_filePath);
            }
            catch (Exception) {
                NetworkCommunicationManagers.Disconnect(controlSocket);
                NetworkCommunicationManagers.Disconnect(dataSocket);
                lock (RunningTransfers) {
                    if(fileTransferContainer.status != FileTransferStatus.Cancelled) {
                        fileTransferContainer.status = FileTransferStatus.Error;
                    }                                        
                }
                return;
            }
        }

        internal bool AcceptFileTransfer(Socket socket, int port1, MainWindow mainWindow)
        {
            int _port2;
            SocketException _socketException;

            if (!NetworkCommunicationManagers.ConnectToEndPoint(port1, socket.RemoteEndPoint.ToString().Remove(socket.RemoteEndPoint.ToString().LastIndexOf(':')), out dataSocket, out _socketException)) {
                return false;
            }

            NetworkCommunicationManagers.ReceiveIntOverSocket(dataSocket, out _port2);

            if (!NetworkCommunicationManagers.ConnectToEndPoint(_port2, socket.RemoteEndPoint.ToString().Remove(socket.RemoteEndPoint.ToString().LastIndexOf(':')), out controlSocket, out _socketException)) {
                return false;
            }

            fileTransferContainer.FileTransferClassInstance = this;

            byte[] _buffer = null;
            int _temp;
            long _size;
            string _filename;

            try {
                NetworkCommunicationManagers.ReceiveIntOverSocket(dataSocket, out _temp);
                NetworkCommunicationManagers.ReceiveByteArrayOverSocket(dataSocket, out _buffer, _temp);
            }
            catch (ObjectDisposedException) {
                NetworkCommunicationManagers.Disconnect(dataSocket);
                NetworkCommunicationManagers.Disconnect(controlSocket);
                return false;
            }
            catch (SocketException) {
                NetworkCommunicationManagers.Disconnect(dataSocket);
                NetworkCommunicationManagers.Disconnect(controlSocket);
                return false;
            }

            unpackFileMetadata(_buffer, out _filename, out _size);

            fileTransferContainer.fileName = _filename;
            fileTransferContainer.sizeInBytes = _size;
            fileTransferContainer.size = Converters.DataConverter.bytesToReadableString(_size);
                     
            if (_size > mainWindow.maxAcceptedFileSizeWithoutConfirmation) {
                ManualResetEvent _replyRecieved = new ManualResetEvent(false);
                System.Windows.MessageBoxResult _continue = System.Windows.MessageBoxResult.No;
                mainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                    Thread.Sleep(1000);
                    _continue = System.Windows.MessageBox.Show("Receive file \"" + _filename + "\" from " + socket.RemoteEndPoint.ToString().Remove(socket.RemoteEndPoint.ToString().LastIndexOf(':')) + " (Size: "+ _size + " bytes)", "File Transfer", System.Windows.MessageBoxButton.YesNo);
                    _replyRecieved.Set();
                }));
                _replyRecieved.WaitOne();
                _replyRecieved.Dispose();
                if (_continue != System.Windows.MessageBoxResult.Yes) {
                    mainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                        lock (RunningTransfers) {
                            RunningTransfers.Remove(fileTransferContainer);
                        }
                    }));                    
                    NetworkCommunicationManagers.Disconnect(dataSocket);
                    NetworkCommunicationManagers.Disconnect(controlSocket);
                    return false;
                }
            }

            if(!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ChatApp"))){
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ChatApp"));
            };

            string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ChatApp" , _filename);
            
            if (File.Exists(_filePath)) {
                string _extension, _assignedFilename;
                if (_filename.LastIndexOf('.') == -1) {
                    _extension = "";
                    _assignedFilename = _filename;
                }
                else {
                    _extension = "." + _filename.Remove(0, _filename.LastIndexOf('.') + 1);
                    _assignedFilename = _filename.Remove(_filename.Length - _extension.Length, _extension.Length);
                }
                
                for (int i = 0; true ; i++) {
                    _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ChatApp" , _assignedFilename + "(" + i + ")" + _extension);
                    if (!File.Exists(_filePath)) {
                        break;
                    }
                }
            }
            return managedReceiveFileOverSockets(_filePath, _size);
        }
    }
}
