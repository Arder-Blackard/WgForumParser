using System;
using System.Windows;
using System.Windows.Controls;

namespace ForumParser.Views.Controls
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

        //        protected override void OnRenderSizeChanged()
        //        {
        //            
        //        }
        /// <summary>
        ///     Invoked when the parent of this element in the visual tree is changed. Overrides
        ///     <see cref="M:System.Windows.UIElement.OnVisualParentChanged(System.Windows.DependencyObject)" />.
        /// </summary>
        /// <param name="oldParent">The old parent element. May be null to indicate that the element did not have a visual parent previously.</param>
        protected override void OnVisualParentChanged( DependencyObject oldParent )
        {
            base.OnVisualParentChanged( oldParent );

            var parent = oldParent as UIElement;
            if ( parent != null )
                parent.LayoutUpdated -= Parent_LayoutUpdated;

            parent = VisualParent as UIElement;
            if ( parent != null )
                parent.LayoutUpdated += Parent_LayoutUpdated;
        }

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

        public event EventHandler ParentLayoutUpdated;

        #endregion


        #region Event handlers

        private void Parent_LayoutUpdated( object sender /* always null */, EventArgs args ) => ParentLayoutUpdated?.Invoke( sender, args );

        #endregion


        #region Non-public methods

        private void UpdateHotspot()
        {
            HotspotLocation = new Point( ActualWidth*Hotspot.X, ActualHeight*Hotspot.Y );
        }

        #endregion
    }
}
