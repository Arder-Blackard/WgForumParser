using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ForumParser.ViewModels.Windows;
using ForumParser.Views.Controls;
using ForumParser.Views.Extensions;
using WpfCommon.Views.Base;

namespace ForumParser.Views.Windows
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase<MainWindowViewModel>
    {
        #region Properties

        public IEnumerable<BitmapSource> TemplateImages
        {
            get
            {
                foreach ( var view in ChartsList.EnumerateChildren( true ).OfType<ChartTemplateView>() )
                {
                    view.GripVisible = false;
                    var rtb = new RenderTargetBitmap( (int) view.ActualWidth, (int) view.ActualHeight, 96, 96, PixelFormats.Pbgra32 );
                    rtb.Render( (Visual) view.Content );
                    view.GripVisible = true;
                    yield return rtb;
                }
            }
        }

        #endregion


        #region Initialization

        public MainWindow( MainWindowViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }

        #endregion


        #region Event handlers

        private void UsersGrid_PreviewKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.IsDown )
            {
                if ( e.Key == Key.Delete )
                {
                    ViewModel.DeleteSelectedUserCommand?.Execute( null );
                    FocusSelectedUser();
                }
                else if ( e.Key == Key.Z && Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                {
                    ViewModel.UndoDeleteUserCommand?.Execute( null );
                    AllUsersGrid.UpdateLayout();
                    Dispatcher.Invoke( FocusSelectedUser, DispatcherPriority.Loaded );
                }
            }
        }

        #endregion


        #region Non-public methods

        private void FocusSelectedUser()
        {
            var gridRow = (UIElement) AllUsersGrid.ItemContainerGenerator.ContainerFromItem( ViewModel.SelectedUser );
            var gridCell = gridRow?.EnumerateChildren( true ).OfType<DataGridCell>().FirstOrDefault();
            gridCell?.Focus();
        }

        #endregion
    }
}
