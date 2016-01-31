using System.Net.Sockets;

namespace ChatApplication.Network
{
    public static partial class NetworkCommunicationManagers
    {
        static internal bool ReceiveIntOverSocket(Socket socket, out int msg)
        {
            msg = 0;
            try {
                int _readSoFar = 0, _size = 4;
                byte[] _buffer = new byte[4];
                while (_readSoFar < _size) {
                    int _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    _readSoFar += _read;
                    if (_read == 0) {
                        // connection was broken
                        return false;
                    }
                }

                msg = System.Net.IPAddress.NetworkToHostOrder(System.BitConverter.ToInt32(_buffer, 0));
                return true;
            }
            catch (System.Exception) {
                return false;
            }            
        }

        static internal bool ReceiveStringOverSocket(Socket socket, out string msg, int length)
        {
            msg = string.Empty;
            try {
                int _readSoFar = 0, _size = length;
                byte[] _buffer = new byte[_size];
                while (_readSoFar < _size) {
                    int _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    _readSoFar += _read;
                    if (_read == 0) {
                        // connection was broken
                        return false;
                    }
                }
                msg = System.Text.Encoding.ASCII.GetString(_buffer);
                return true;
            }
            catch (System.Exception) {
                return false;
            }
        }

        static internal bool ReceiveByteArrayOverSocket(Socket socket, out byte[] msg, int length)
        {
            msg = null;
            try {
                int _readSoFar = 0, _size = length;
                byte[] _buffer = new byte[_size];
                while (_readSoFar < _size) {
                    int _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    _readSoFar += _read;
                    if (_read == 0) {
                        // connection was broken
                        return false;
                    }
                }
                msg = _buffer;
                return true;
            }
            catch (System.Exception) {
                return false;
            }
        }
    }
}