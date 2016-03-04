using System.Net.Sockets;
using ChatApplication;

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

        static internal bool SendEncryptedStringOverSocket(Socket socket, byte[] key, string msg)
        {
            byte[] _temp, _buffer, _iv;
            int _sentSoFar, _size;

            _temp = Encryption.EncodeStringUsingAes(key, msg, out _iv);
            _buffer = new byte[_temp.Length + _iv.Length];
            System.Buffer.BlockCopy(_temp, 0, _buffer, 0, _temp.Length);
            System.Buffer.BlockCopy(_iv, 0, _buffer, _temp.Length, _iv.Length);
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

        static internal bool SendEncryptedIntOverSocket(Socket socket, byte[] key, int msg)
        {
            byte[] _temp,_buffer, _iv;
            int _sentSoFar, _size;

            _temp = Encryption.EncodeByteArrayUsingAes(key, System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(msg)), out _iv);
            _buffer = new byte[_temp.Length + _iv.Length];
            System.Buffer.BlockCopy(_temp, 0, _buffer, 0, _temp.Length);
            System.Buffer.BlockCopy(_iv, 0, _buffer, _temp.Length, _iv.Length);
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

        static internal bool SendEncryptedByteArrayOverSocket(Socket socket, byte[] key, byte[] msg)
        {
            int _sentSoFar, _size;
            byte[] _temp, _iv;

            _temp = Encryption.EncodeByteArrayUsingAes(key, msg, out _iv);
            msg = new byte[_temp.Length + _iv.Length];
            System.Buffer.BlockCopy(_temp, 0, msg, 0, _temp.Length);
            System.Buffer.BlockCopy(_iv, 0, msg, _temp.Length, _iv.Length);
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