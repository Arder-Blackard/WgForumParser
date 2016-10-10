using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForumParser.Views.Controls
{
    /// <summary>
    ///     Interaction logic for RL.xaml
    /// </summary>
    public class RotatableLabel : ContentControl
    {
        #region Constants

        public static readonly DependencyProperty RotationAllowedProperty =
            DependencyProperty.Register( "RotationAllowed", typeof ( bool ), typeof ( RotatableLabel ),
                                         new FrameworkPropertyMetadata( default(bool), FrameworkPropertyMetadataOptions.AffectsMeasure ) );

        #endregion


        #region Properties

        public bool RotationAllowed
        {
            get { return (bool) GetValue( RotationAllowedProperty ); }
            set { SetValue( RotationAllowedProperty, value ); }
        }

        #endregion


        #region Non-public methods

        protected override Size MeasureOverride( Size constraint )
        {
            if ( VisualChildrenCount > 0 )
            {
                var child = (FrameworkElement) GetVisualChild( 0 );
                if ( child != null )
                {
                    if ( !RotationAllowed )
                    {
                        child.Measure( constraint );
                    }
                    else
                    {

                        child.LayoutTransform = Transform.Identity;
                        child.Measure( new Size( double.PositiveInfinity, double.PositiveInfinity ) );


                        var size = child.DesiredSize;
                        if ( size.Width > constraint.Width )
                        {
                            var diagonal = Math.Sqrt( size.Width*size.Width + size.Height*size.Height );
                            var angle = Math.Acos( constraint.Width/diagonal )*180/Math.PI;
                            child.LayoutTransform = new RotateTransform( angle, 0, 0 );
                            child.Measure( constraint );
                        }
                    }

                    return child.DesiredSize;
                }
            }

            return new Size( 0.0, 0.0 );
        }

        #endregion
    }
}
