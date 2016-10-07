using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForumParser.Views.Extensions
{
    public static class VisualExtensions
    {
        #region Public methods

        public static IEnumerable<DependencyObject> EnumerateChildren( this Visual element, bool recursive = false )
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount( element );
            for ( var i = 0; i < childrenCount; i++ )
            {
                var child = VisualTreeHelper.GetChild( element, i );
                yield return child;

                if ( recursive )
                {
                    var visual = child as Visual;
                    if ( visual != null )
                    {
                        foreach ( var grandChild in visual.EnumerateChildren( true ) )
                            yield return grandChild;
                    }
                }
            }
        }

        public static TChildType FindChildOfType<TChildType>( this Visual element, bool recursive = false ) where TChildType : DependencyObject
        {
            return EnumerateChildren( element, recursive ).OfType<TChildType>().FirstOrDefault();
        }

        public static UIElement FindItemContainer( this ItemsControl itemsControl, object item )
        {
            return (UIElement) itemsControl.ItemContainerGenerator.ContainerFromItem( item );
        }

        #endregion
    }
}
