namespace ChatApplication.DataContainers
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    struct ConnectedPeerDataContainer
    {
        public System.Net.Sockets.Socket socket;
        public string nick;
        public string encodedMachineName;
    }
}
