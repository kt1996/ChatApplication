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
                    int _read;
                    try {
                        _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    }
                    catch (SocketException ex) {
                        if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                            System.Threading.Thread.Sleep(30);
                            continue;
                        }
                        else {
                            throw ex;
                        }
                    }
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
                    int _read;
                    try {
                        _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    }
                    catch (SocketException ex) {
                        if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                            System.Threading.Thread.Sleep(30);
                            continue;
                        }
                        else {
                            throw ex;
                        }
                    } 
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
                    int _read;
                    try {
                        _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    }
                    catch (SocketException ex) {
                        if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                            System.Threading.Thread.Sleep(30);
                            continue;
                        }
                        else {
                            throw ex;
                        }
                    } 
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

        static internal bool ReceiveDecodedIntOverSocket(Socket socket, byte[] key, out int msg)
        {
            msg = 0;
            try {
                int _readSoFar = 0, _size = 32;
                byte[] _buffer = new byte[32];
                while (_readSoFar < _size) {
                    int _read;
                    try {
                        _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    }
                    catch (SocketException ex) {
                        if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                            System.Threading.Thread.Sleep(30);
                            continue;
                        }
                        else {
                            throw ex;
                        }
                    }
                    _readSoFar += _read;
                    if (_read == 0) {
                        // connection was broken
                        return false;
                    }
                }

                byte[] _iv = new byte[16];
                byte[] _temp;
                System.Buffer.BlockCopy(_buffer, _buffer.Length - 16, _iv, 0, 16);
                _temp = new byte[_buffer.Length - 16];
                System.Buffer.BlockCopy(_buffer, 0, _temp, 0, _temp.Length);
                _buffer = Encryption.DecodeToByteArrayUsingAes(key, _temp, _iv);

                msg = System.Net.IPAddress.NetworkToHostOrder(System.BitConverter.ToInt32(_buffer, 0));
                return true;
            }
            catch (System.Exception) {
                return false;
            }
        }

        static internal bool ReceiveDecodedStringOverSocket(Socket socket, byte[] key, out string msg, int length)
        {
            msg = string.Empty;
            try {
                int _readSoFar = 0, _size = (((int)(length/16))+2)*16;
                byte[] _buffer = new byte[_size];
                while (_readSoFar < _size) {
                    int _read;
                    try {
                        _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    }
                    catch (SocketException ex) {
                        if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                            System.Threading.Thread.Sleep(30);
                            continue;
                        }
                        else {
                            throw ex;
                        }
                    }
                    _readSoFar += _read;
                    if (_read == 0) {
                        // connection was broken
                        return false;
                    }
                }

                byte[] _iv = new byte[16];
                byte[] _temp;
                System.Buffer.BlockCopy(_buffer, _buffer.Length - 16, _iv, 0, 16);
                _temp = new byte[_buffer.Length - 16];
                System.Buffer.BlockCopy(_buffer, 0, _temp, 0, _temp.Length);
                _buffer = Encryption.DecodeToByteArrayUsingAes(key, _temp, _iv);

                msg = System.Text.Encoding.ASCII.GetString(_buffer);
                return true;
            }
            catch (System.Exception) {
                return false;
            }
        }

        static internal bool ReceiveDecodedByteArrayOverSocket(Socket socket, byte[] key, out byte[] msg, int length)
        {
            msg = null;
            try {
                int _readSoFar = 0, _size = (((int)(length / 16)) + 2) * 16;
                byte[] _buffer = new byte[_size];
                while (_readSoFar < _size) {
                    int _read;
                    try {
                        _read = socket.Receive(_buffer, _readSoFar, _size - _readSoFar, SocketFlags.None);
                    }
                    catch (SocketException ex) {
                        if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                            System.Threading.Thread.Sleep(30);
                            continue;
                        }
                        else {
                            throw ex;
                        }
                    }
                    _readSoFar += _read;
                    if (_read == 0) {
                        // connection was broken
                        return false;
                    }
                }

                byte[] _iv = new byte[16];
                byte[] _temp;
                System.Buffer.BlockCopy(_buffer, _buffer.Length - 16, _iv, 0, 16);
                _temp = new byte[_buffer.Length - 16];
                System.Buffer.BlockCopy(_buffer, 0, _temp, 0, _temp.Length);
                msg = Encryption.DecodeToByteArrayUsingAes(key, _temp, _iv);
                
                return true;
            }
            catch (System.Exception) {
                return false;
            }
        }
    }
}