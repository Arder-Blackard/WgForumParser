using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForumParser.ViewModels.Controls;

namespace ForumParser.Views.Controls
{
    /// <summary>
    ///     Interaction logic for MergedChart.xaml
    /// </summary>
    public partial class ChartTemplateView : UserControl
    {
        #region Fields

        private Vector _initialDelta;
        private Size _initialSize;
        private Point _resizeOrigin;
        private bool _resizing;

        #endregion


        #region Initialization

        public ChartTemplateView()
        {
            InitializeComponent();
        }

        #endregion


        #region Event handlers

        private void Grip_MouseMove( object sender, MouseEventArgs e )
        {
            if ( _resizing )
            {
                var desiredSize = _initialDelta + Mouse.GetPosition( this );
                var templateViewModel = DataContext as EditableChartTemplateViewModel;
                if ( templateViewModel != null )
                {
                    templateViewModel.Width = Math.Max( desiredSize.X, 200 );
                    templateViewModel.Height = Math.Max( desiredSize.Y, 200 );
                }
            }
        }

        private void Grip_MouseDown( object sender, MouseButtonEventArgs e )
        {
            if ( e.LeftButton == MouseButtonState.Pressed )
            {
                _resizing = true;
                _resizeOrigin = Mouse.GetPosition( this );
                _initialSize = RenderSize;
                _initialDelta = new Vector( _initialSize.Width - _resizeOrigin.X, _initialSize.Height - _resizeOrigin.Y );
                Mouse.Capture( SizeGrip, CaptureMode.SubTree );
            }
        }

        private void Grip_MouseUp( object sender, MouseButtonEventArgs e )
        {
            if ( e.LeftButton == MouseButtonState.Released )
            {
                _resizing = false;
                Mouse.Capture( null );
            }
        }

        #endregion
    }
}
