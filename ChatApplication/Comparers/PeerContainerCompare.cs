using ChatApplication.DataContainers;

namespace ChatApplication.Comparers
{
    static class PeerContainerCompare
    {
        public static int CompareAscending(PeerDataContainer a, PeerDataContainer b)
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
                else {
                    int _resultOfComparison = string.CompareOrdinal(a.nick, b.nick);
                if(_resultOfComparison < 0) {
                    return -1;
                }else if(_resultOfComparison > 0) {
                    return 1;
                }
                else {
                    return 0;
                }
                }

            }
        }

        public static int CompareDescending(PeerDataContainer a, PeerDataContainer b)
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
