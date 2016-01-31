using System.Net.Sockets;

namespace ChatApplication.Network
{
    public static partial class NetworkCommunicationManagers
    {
        static readonly ushort[] TCPPorts = { 5198, 9018, 9019, 9020, 9056 };

        static internal bool ConnectToEndPoint(string address, out Socket socket, out SocketException exception)
        {
            socket = null;
            exception = null;
            short _numberOfTries = 1;
            bool _isConnected = false;
            SocketException _exception = null;
            while (_numberOfTries <= 3) {
                for (int portOffset = 0; portOffset < TCPPorts.Length; portOffset++) {
                    Socket _peerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    System.Threading.Thread thread = new System.Threading.Thread(() => ConnectToParticularEndpoint(ref _isConnected, ref _exception, ref _peerSocket, portOffset, address));
                    thread.Name = "Attempt connection to " + address;
                    thread.IsBackground = true;
                    thread.Start();
                    thread.Join(1000);

                    //connected
                    if (_isConnected) {
                        socket = _peerSocket;
                        return true;
                    }

                    //Some other exception occurs
                    if (_exception != null && _exception.ErrorCode != 10061 && _exception.ErrorCode != 10065 && _exception.ErrorCode != 10060 && _exception.ErrorCode != 10064) {
                        exception = _exception;
                        return false;
                    }
                }
            }
            return false;
        }

        static internal void ConnectToParticularEndpoint(ref bool connected, ref SocketException ex, ref Socket _peerSocket, int portOffset, string address)
        {
            System.Net.IPEndPoint client_endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(address), TCPPorts[portOffset]);
            try {
                _peerSocket.Connect(client_endpoint);
                connected = true;
            }
            catch (SocketException e) {
                ex = e;
            }
        }

        static internal void Disconnect(Socket socket)
        {
            try {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (System.Exception) {
                try {
                    socket.Close();
                }
                catch (System.Exception) { }
            }
        }

        static internal short TryBindServer(out Socket serverSocket)
        {
            short _pos = 0;
            while (true) {
                try {
                    System.Net.IPEndPoint server_endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, TCPPorts[_pos]);
                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    serverSocket.Bind(server_endpoint);
                    return _pos;
                }
                catch (System.Exception) {
                    if (_pos == (TCPPorts.Length - 1)) {
                        serverSocket = null;
                        return -1;
                    }
                    else {
                        _pos++;
                    }
                }
            }
        }
    }
}