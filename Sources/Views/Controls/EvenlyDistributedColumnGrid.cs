using System;
using System.Windows;
using System.Windows.Controls;

namespace ForumParserWPF.Views.Controls
{
    public class EvenlyDistributedColumnGrid : Panel
    {
        #region Constants

        public static readonly DependencyProperty IsReverseOrderProperty =
            DependencyProperty.Register( "IsReverseOrder",
                                         typeof( bool ),
                                         typeof( EvenlyDistributedColumnGrid ),
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
            var largestElementHeight = 0.0;
            foreach ( UIElement child in Children )
            {
                child.Measure( availableSize );
                if ( child.DesiredSize.Height > largestElementHeight )
                    largestElementHeight = child.DesiredSize.Height;
            }
            var result = base.MeasureOverride( availableSize );

            return largestElementHeight > 0.0 ? new Size( result.Width, largestElementHeight ) : result;
        }

        protected override Size ArrangeOverride( Size finalSize )
        {
            var cellSize = new Size( finalSize.Width/Children.Count, Math.Ceiling( finalSize.Height ) );
            var column = 0;
            var reverseStartPoint = finalSize.Width - cellSize.Width;
            foreach ( UIElement child in Children )
            {
                var rowHeight = IsReverseOrder ? reverseStartPoint - cellSize.Width*column : cellSize.Width*column;
                child.Arrange( new Rect( new Point( rowHeight, 0 ), cellSize ) );
                column++;
            }
            return finalSize;
        }

        #endregion
    }
}