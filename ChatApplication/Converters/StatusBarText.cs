using System;

namespace ChatApplication.Converters
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class StatusBarText : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string _status;

            if (parameter == null || value == null) {
                return "";
            }

            if ((bool)value == true) {
                _status = "Yes";
            }
            else {
                _status = "No";
            }

            //For Server Status
            if (int.Parse((string)parameter) == 0) {
                return "Server Running: " + _status;
            }

            //For Broadcasting status
            else {
                return "Status Broadcasting: " + _status;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
