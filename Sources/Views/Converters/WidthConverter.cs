using System;
using System.Globalization;
using System.Windows.Data;

namespace ForumParser.Views.Converters
{
    public class WidthConverter : IValueConverter
    {
        #region Fields

        public static readonly WidthConverter Instance = new WidthConverter();

        #endregion


        #region Public methods

        public object Convert( object value, Type type, object parameter, CultureInfo culture )
        {
            return System.Convert.ToDouble( value ) - System.Convert.ToDouble( parameter );
        }

        public object ConvertBack( object o, Type type, object parameter, CultureInfo culture )
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
