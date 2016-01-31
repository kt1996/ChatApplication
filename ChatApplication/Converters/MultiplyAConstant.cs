using System;

namespace ChatApplication.Converters
{
    /*-----------------------------------------------------------------------------------------------------
    Description
    -----------------------------------------------------------------------------------------------------*/

    public class MultiplyAConstant : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double _val;
            if (parameter == null || value == null) {
                return 0;
            }
            else {
                try {
                    _val = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
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
                    _val = System.Convert.ToDouble(value) / System.Convert.ToDouble(parameter);
                    return _val;
                }
                catch {
                    return 0;
                }
            }
        }
    }
}
