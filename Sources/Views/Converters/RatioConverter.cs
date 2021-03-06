﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ForumParser.Views.Converters
{
    public class RatioConverter : IMultiValueConverter
    {
        #region Constants

        public static readonly RatioConverter Instance = new RatioConverter();

        #endregion


        #region Public methods

        public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
        {
            try
            {
                if ( values.Length == 2 )
                    return System.Convert.ToDouble( values[0] )*System.Convert.ToDouble( values[1] );
                if ( values.Length == 3 )
                    return System.Convert.ToDouble( values[0] )*(System.Convert.ToDouble( values[1] )/System.Convert.ToDouble( values[2] ));
                return DependencyProperty.UnsetValue;
            }
            catch ( Exception )
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
