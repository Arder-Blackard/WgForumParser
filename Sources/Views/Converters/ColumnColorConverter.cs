using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CommonLib.Extensions;

namespace ForumParser.Views.Converters
{
    public class ColumnColorConverter : IValueConverter
    {
        #region Public methods

        private static readonly Color[] ColumnColors =
        {
            Color.FromRgb( 47, 83, 124 ),
            Color.FromRgb( 127, 48, 45 ),
            Color.FromRgb( 102, 125, 51 ),
            Color.FromRgb( 82, 64, 105 ),
            Color.FromRgb( 42, 114, 132 ),
            Color.FromRgb( 184, 88, 8 ),
        };

        private static readonly Dictionary<int, Brush> ColumnBrushes = new Dictionary<int, Brush>();

        /// <summary>
        ///     Converts a value.
        /// </summary>
        /// <returns>
        ///     A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            try
            {
                var index = System.Convert.ToInt32( value );
                return ColumnBrushes.GetOrInsert( index, () => new SolidColorBrush( ColumnColors[index%ColumnColors.Length] ) );
            }
            catch ( InvalidCastException )
            {
                return DependencyProperty.UnsetValue;
            }
        }

        /// <summary>
        ///     Converts a value.
        /// </summary>
        /// <returns>
        ///     A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
