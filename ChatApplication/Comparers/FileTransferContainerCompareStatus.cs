namespace ChatApplication.Comparers
{
    class FileTransferContainerCompareStatus
    {
        public static int CompareAscending(DataContainers.FileTransferContainer a, DataContainers.FileTransferContainer b)
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
                else if(a.status == b.status) {
                    if(a.status == FileTransferStatus.Paused) {
                        if(a.pausedBy == b.pausedBy) {
                            return 0;
                        }else if(a.pausedBy > b.pausedBy) {
                            return 1;
                        }
                        else {
                            return -1;
                        }
                    }
                    else if(a.transferType == b.transferType) {
                        return 0;
                    }else if(a.transferType > b.transferType) {
                        return 1;
                    }
                    else {
                        return -1;
                    }
                }else if(a.status < b.status) {
                    return 1;
                }
                else {
                    return -1;
                }

            }
        }

        public static int CompareDescending(DataContainers.FileTransferContainer a, DataContainers.FileTransferContainer b)
        {
            int result = CompareAscending(a, b);
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
