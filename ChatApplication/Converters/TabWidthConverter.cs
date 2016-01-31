using System;

namespace ChatApplication.Converters
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class TabWidthConverter : System.Windows.Data.IValueConverter
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
                    double max = double.Parse(_parameters[1]);
                    _val = System.Convert.ToDouble(value);
                    if (((1 - multiplier) * _val) > max) {
                        _val = (_val - max) - 20;
                        if (_val < 0) {
                            return 0;
                        }
                        return _val;
                    }
                    else {
                        _val = (multiplier * _val) - 20;
                        if (_val < 0) {
                            return 0;
                        }
                        return _val;
                    }
                }
                catch {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
