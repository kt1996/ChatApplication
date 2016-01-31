using System.Net.Sockets;

namespace ChatApplication.Network
{
    public static partial class NetworkCommunicationManagers
    {
        static internal bool SendStringOverSocket(Socket socket, string msg)
        {
            byte[] _buffer;
            int _sentSoFar, _size;

            _buffer = System.Text.Encoding.ASCII.GetBytes(msg);
            _sentSoFar = 0;
            _size = _buffer.Length;
            while (_sentSoFar < _size) {
                int _sent = socket.Send(_buffer, _sentSoFar, _size - _sentSoFar, SocketFlags.None);
                _sentSoFar += _sent;
                if (_sent == 0) {
                    // connection was broken
                    return false;
                }
            }
            return true;
        }

        static internal bool SendIntOverSocket(Socket socket, int msg)
        {
            byte[] _buffer;
            int _sentSoFar, _size;

            _buffer = new byte[4];
            _buffer = System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(msg));
            _sentSoFar = 0;
            _size = 4;
            while (_sentSoFar < _size) {
                int _sent = socket.Send(_buffer, _sentSoFar, _size - _sentSoFar, SocketFlags.None);
                _sentSoFar += _sent;
                if (_sent == 0) {
                    // connection was broken
                    return false;
                }
            }
            return true;
        }

        static internal bool SendByteArrayOverSocket(Socket socket, byte[] msg)
        {
            int _sentSoFar, _size;

            _sentSoFar = 0;
            _size = msg.Length;
            while (_sentSoFar < _size) {
                int _sent = socket.Send(msg, _sentSoFar, _size - _sentSoFar, SocketFlags.None);
                _sentSoFar += _sent;
                if (_sent == 0) {
                    // connection was broken
                    return false;
                }
            }
            return true;
        }
    }
}