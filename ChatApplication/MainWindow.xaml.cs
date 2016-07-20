using ChatApplication.DataContainers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ChatApplication
{
    /// check file transfer before closing
    /// ctrl + w to close tab

    public partial class MainWindow : Window
    {

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Window variables
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        private bool isLogWindowOpen = false;

        private GridViewColumnHeader listViewSortCol = null;
        private Graphics.Adorners.SortAdorner listViewSortAdorner = null;

        private List<PeerDataContainer> broadcastingPeersList = new List<PeerDataContainer>();

        private System.ComponentModel.BindingList<FileTransferContainer> RunningTransfers = new System.ComponentModel.BindingList<FileTransferContainer>();
        internal Dialogs.FileTransferWindow fileTransferWindow;

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        //Back-end variables
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        private List<ConnectedPeerDataContainer> connectedPeersList = new List<ConnectedPeerDataContainer>();
        private Socket serverSocket;
        private ushort[] TCPPorts = { 5198, 9018, 9019, 9020, 9056 };
        private short numberOfBroadcastsSinceListUpdate = 0;
        private bool isServerRunning = false;
        private bool isBroadcasting = true;
        internal long maxAcceptedFileSizeWithoutConfirmation = 5242880;
        internal string nick = "";
        private string password = null;
        private string encodedMachineName;
        private UdpClient broadcastReceiver;
        List<IPAddress> broadcastIPs = new List<IPAddress>();
        Timer updateBroadcastListTimer, broadcastTimer;
        
        public MainWindow()
        {
            InitializeComponent();

            //Get and encode Machine Name
            byte[] _hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(new UTF8Encoding().GetBytes(Environment.MachineName));
            encodedMachineName = BitConverter.ToString(_hash).Replace("-", string.Empty).ToLower();

            //Start the server for receiving requests
            StartServer();

            //Start Receiving Broadcasts
            ReceiveBroadcasts();

            //Update clients list periodically
            updateBroadcastListTimer = new Timer(UpdateAvailableClients, null, 0, 20000);

            Network.FileTransfer.RunningTransfers = RunningTransfers;
            Network.FileTransfer.mainWindow = this;
        }  
    }
}

