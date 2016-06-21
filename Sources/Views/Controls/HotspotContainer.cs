using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForumParserWPF.Views.Controls
{
    public class HotspotContainer : ContentControl
    {
        #region Constants

        public static readonly DependencyProperty HotspotProperty = DependencyProperty.Register(
            "Hotspot", typeof ( Point ), typeof ( HotspotContainer ),
            new FrameworkPropertyMetadata( default(Point), ( d, args ) => (d as HotspotContainer)?.UpdateHotspot() ) );

        public static readonly DependencyProperty HotspotLocationProperty = DependencyProperty.Register(
            "HotspotLocation", typeof ( Point ), typeof ( HotspotContainer ), new FrameworkPropertyMetadata( default(Point) ) );

        #endregion


        #region Properties

        public Point Hotspot
        {
            get { return (Point) GetValue( HotspotProperty ); }
            set { SetValue( HotspotProperty, value ); }
        }

        public Point HotspotLocation
        {
            get { return (Point) GetValue( HotspotLocationProperty ); }
            set { SetValue( HotspotLocationProperty, value ); }
        }

        #endregion


        #region Events and invocation

        /// <summary>
        ///     Raises the <see cref="E:System.Windows.FrameworkElement.SizeChanged" /> event, using the specified information as part of the eventual
        ///     event data.
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        protected override void OnRenderSizeChanged( SizeChangedInfo sizeInfo )
        {
            base.OnRenderSizeChanged( sizeInfo );
            UpdateHotspot();
        }

        /// <summary>
        /// Supports incremental layout implementations in specialized subclasses of <see cref="T:System.Windows.FrameworkElement"/>. <see cref="M:System.Windows.FrameworkElement.ParentLayoutInvalidated(System.Windows.UIElement)"/>  is invoked when a child element has invalidated a property that is marked in metadata as affecting the parent's measure or arrange passes during layout. 
        /// </summary>
        /// <param name="child">The child element reporting the change.</param>
        protected override void ParentLayoutInvalidated( UIElement child )
        {
            base.ParentLayoutInvalidated( child );
            UpdateHotspot();
        }

        private void UpdateHotspot()
        {
           HotspotLocation = new Point(ActualWidth * Hotspot.X, ActualHeight * Hotspot.Y);
        }

        #endregion


        #region Initialization

        #endregion


        #region Event handlers

        #endregion
    }
}
