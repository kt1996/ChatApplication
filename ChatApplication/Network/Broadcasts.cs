using System.Net.Sockets;

namespace ChatApplication.Network
{
    public static partial class NetworkCommunicationManagers
    {
        static internal void UpdateBroadcastList(out System.Collections.Generic.List<System.Net.IPAddress> broadcastIPs)
        {
            broadcastIPs = new System.Collections.Generic.List<System.Net.IPAddress>();
            System.Net.IPAddress[] _localIPs = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
            broadcastIPs.Clear();
            broadcastIPs.Add(System.Net.IPAddress.Broadcast);

            foreach (System.Net.IPAddress ipaddress in _localIPs) {
                if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                    System.Net.IPAddress _subnetMask = null;
                    foreach (System.Net.NetworkInformation.NetworkInterface _adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
                        foreach (System.Net.NetworkInformation.UnicastIPAddressInformation _unicastIPAddressInformation in _adapter.GetIPProperties().UnicastAddresses) {
                            if (_unicastIPAddressInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                                if (ipaddress.Equals(_unicastIPAddressInformation.Address)) {
                                    _subnetMask = _unicastIPAddressInformation.IPv4Mask;
                                }
                            }
                        }
                    }
                    if (_subnetMask != null) {
                        byte[] _ipAdressBytes = ipaddress.GetAddressBytes();
                        byte[] _subnetMaskBytes = _subnetMask.GetAddressBytes();

                        if (_ipAdressBytes.Length != _subnetMaskBytes.Length)
                            throw new System.ArgumentException("Lengths of IP address and subnet mask do not match.");

                        byte[] broadcastAddress = new byte[_ipAdressBytes.Length];
                        for (int i = 0; i < broadcastAddress.Length; i++) {
                            broadcastAddress[i] = (byte)(_ipAdressBytes[i] | (_subnetMaskBytes[i] ^ 255));
                        }
                        broadcastIPs.Add(new System.Net.IPAddress(broadcastAddress));
                    }
                }
            }
        }

        static internal void BroadcastFromList(string msg, System.Collections.Generic.List<System.Net.IPAddress> broadcastIPs)
        {
            foreach (System.Net.IPAddress address in broadcastIPs) {
                UdpClient client = new UdpClient();
                System.Net.IPEndPoint ip = new System.Net.IPEndPoint(address, 15069);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(msg);

                try {
                    client.Send(bytes, bytes.Length, ip);
                    client.Close();
                }
                catch (System.Exception) {  }
            }
        }
    }
}