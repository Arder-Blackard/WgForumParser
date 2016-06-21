using System;
using System.Windows;
using System.Windows.Controls;

namespace ForumParserWPF.Views.Controls
{
    public class EvenlyDistributedRowGrid : Panel
    {
        #region Constants

        public static readonly DependencyProperty IsReverseOrderProperty =
            DependencyProperty.Register( "IsReverseOrder",
                                         typeof( bool ),
                                         typeof( EvenlyDistributedRowGrid ),
                                         new PropertyMetadata( false ) );

        #endregion


        #region Properties

        public bool IsReverseOrder
        {
            get { return (bool) GetValue( IsReverseOrderProperty ); }
            set { SetValue( IsReverseOrderProperty, value ); }
        }

        #endregion


        #region Non-public methods

        protected override Size MeasureOverride( Size availableSize )
        {
            var largestElementWidth = 0.0;
            foreach ( UIElement child in Children )
            {
                child.Measure( availableSize );
                if ( child.DesiredSize.Width > largestElementWidth )
                {
                    largestElementWidth = child.DesiredSize.Width;
                }
            }
            var result = base.MeasureOverride( availableSize );

            return largestElementWidth > 0.0 ? new Size( largestElementWidth, result.Height ) : result;
        }

        protected override Size ArrangeOverride( Size finalSize )
        {
            var cellSize = new Size( Math.Ceiling( finalSize.Width ), finalSize.Height/Children.Count );
            var row = 0;
            var reverseStartPoint = finalSize.Height - cellSize.Height;
            foreach ( UIElement child in Children )
            {
                var rowHeight = IsReverseOrder ? reverseStartPoint - cellSize.Height*row : cellSize.Height*row;
                child.Arrange( new Rect( new Point( 0, rowHeight ), cellSize ) );
                row++;
            }
            return finalSize;
        }

        #endregion
    }
}
