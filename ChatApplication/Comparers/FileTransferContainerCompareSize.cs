namespace ChatApplication.Comparers
{
    class FileTransferContainerCompareSize
    {
        public static int CompareDescending(DataContainers.FileTransferContainer a, DataContainers.FileTransferContainer b)
        {
            if (a == null) {
                if (b == null) {
                    return 0;
                }
                else {
                    return -1;
                }
            }
            else {
                if (b == null) {
                    return 1;
                }
                if(a.sizeInBytes == b.sizeInBytes) {
                    return 0;
                }else if(a.sizeInBytes > b.sizeInBytes) {
                    return 1;
                }
                else {
                    return -1;
                }
            }
        }

        public static int CompareAscending(DataContainers.FileTransferContainer a, DataContainers.FileTransferContainer b)
        {
            int result = CompareDescending(a, b);
            if (result == 1) {
                return -1;
            }
            else if (result == -1) {
                return 1;
            }
            else {
                return 0;
            }
        }
    }
}
