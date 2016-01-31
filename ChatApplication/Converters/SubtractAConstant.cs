using System;

namespace ChatApplication.Converters
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class SubtractAConstant : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null) {
                return 0;
            }
            else {
                try {
                    _size = System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter);
                    if (_size < 0) {
                        return 0;
                    }
                    return _size;
                }
                catch {
                    return 0;
                }

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _size;
            if (parameter == null || value == null) {
                return 0;
            }
            else {
                try {
                    _size = System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
                    return _size;
                }
                catch {
                    return 0;
                }
            }
        }
    }
}
