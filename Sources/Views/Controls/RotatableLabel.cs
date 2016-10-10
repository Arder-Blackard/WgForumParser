using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForumParser.Views.Controls
{
    public class RotatableLabel : Label
    {
        protected override Size MeasureOverride( Size constraint )
        {
            if (VisualChildrenCount > 0)
            {
                var child = (FrameworkElement)GetVisualChild(0);
                if (child != null)
                {
                    child.Measure( new Size( double.PositiveInfinity, double.PositiveInfinity ) );

                    var size = child.DesiredSize;
                    if ( size.Width > constraint.Width )
                    {
                        var diagonal = Math.Sqrt( size.Width*size.Width + size.Height*size.Height );
                        var angle = Math.Acos( constraint.Width/diagonal ) * 180 / Math.PI;
                        child.LayoutTransform = new RotateTransform( angle, 0, 0 );
                        child.Measure( constraint );
                    }

                    return child.DesiredSize;
                }
            }

            return new Size(0.0, 0.0);
        }
    }
}