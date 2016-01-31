using System;

namespace ChatApplication.Converters
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class MessageboxWidthConverter : System.Windows.Data.IValueConverter
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
                    double sub = double.Parse(_parameters[2]);
                    _val = System.Convert.ToDouble(value);
                    if (((1 - multiplier) * _val) > max) {
                        return (_val - max) - (33 + sub);
                    }
                    else {
                        return (multiplier * _val) - (33 + sub);
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
