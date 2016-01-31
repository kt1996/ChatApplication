using System;

namespace ChatApplication.Converters
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class SubtractAndThenMultiplyTwoConstants : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null) {
                return 0;
            }
            else {
                try {
                    string _param = parameter.ToString();
                    double _multiplier = double.Parse(_param.Substring(0, _param.IndexOf('-')));
                    int subtractval = int.Parse(_param.Remove(0, _param.IndexOf('-') + 1));
                    _val = (System.Convert.ToDouble(value) - subtractval) * _multiplier;
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
                    string _param = parameter.ToString();
                    double multiplier = double.Parse(_param.Substring(0, _param.IndexOf('-')));
                    int subtractval = int.Parse(_param.Remove(0, _param.IndexOf('-') + 1));
                    _val = (System.Convert.ToDouble(value) / multiplier) + subtractval;
                    return _val;
                }
                catch {
                    return 0;
                }
            }
        }
    }
}
