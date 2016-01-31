using System;

namespace ChatApplication.Converters
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class MultiplyAndThenSubtractTwoConstants : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null) {
                return 0;
            }
            else {
                try {
                    string[] _parameters = ((string)parameter).Split(new char[] { '-' });
                    double multiplier = double.Parse(_parameters[0]);
                    int subtractval = int.Parse(_parameters[1]);
                    _val = (System.Convert.ToDouble(value) * multiplier) - subtractval;
                    if (_val < 0) {
                        return 0;
                    }
                    return _val;
                }
                catch {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null) {
                return 0;
            }
            else {
                try {
                    string[] _parameters = ((string)parameter).Split(new char[] { '-' });
                    double multiplier = double.Parse(_parameters[0]);
                    int subtractval = int.Parse(_parameters[1]);
                    _val = (System.Convert.ToDouble(value) + subtractval) / multiplier;
                    return _val;
                }
                catch {
                    return 0;
                }
            }
        }
    }
}
