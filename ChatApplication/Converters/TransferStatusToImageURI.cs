using System;

namespace ChatApplication.Converters
{
    /*
        Icons from        
        https://www.iconfinder.com/iconsets/Hand_Drawn_Web_Icon_Set
    */

    class TransferStatusToImageURI : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string _file = "/Resources/Images/";

            FileTransferType _transferType = (FileTransferType)values[0];
            FileTransferStatus _status = (FileTransferStatus)values[1];
            PausedBy _pausedBy = (PausedBy)values[2];

            switch (_transferType) {
                case FileTransferType.Download:
                    _file += "Download";
                    break;
                case FileTransferType.Upload:
                    _file += "Upload";
                    break;
            }

            switch (_status) {
                case FileTransferStatus.Cancelled:
                    _file += "Cancelled";
                    break;
                case FileTransferStatus.Error:
                    _file += "Error";
                    break;
                case FileTransferStatus.Finished:
                    _file += "Finished";
                    break;
                case FileTransferStatus.Paused:
                    _file += "Paused";

                    switch (_pausedBy) {
                        case PausedBy.User:
                            _file += "ByUser";
                            break;
                        case PausedBy.OtherPeer:
                            _file += "ByOtherPeer";
                            break;
                        case PausedBy.Both:
                            _file += "ByBoth";
                            break;
                    }
                    break;
                case FileTransferStatus.Running:
                    _file += "Running";
                    break;
            }

            _file += ".png";
            return (new System.Windows.Media.ImageSourceConverter()).ConvertFromString("pack://application:,,," + _file);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
