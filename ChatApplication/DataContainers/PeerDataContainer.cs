using System;

namespace ChatApplication.DataContainers
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class PeerDataContainer
    {
        public string nick { get; set; }
        public string IP { get; set; }
        public string encodedMachineName { get; set; }
        public DateTime time { get; set; }
    }
}
