using System;
using System.Globalization;
using System.Windows.Data;

namespace ForumParser.Views.Converters
{
    public class BoolConverter : IValueConverter
    {
        #region Auto-properties

        public object True { get; set; }
        public object False { get; set; }

        #endregion


        #region Public methods

        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return value as bool? == true ? True : False;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
