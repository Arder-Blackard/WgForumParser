using System;
using System.IO;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase<MainWindowViewModel>
    {
        public MainWindow( MainWindowViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }

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

        private void FocusSelectedUser()
        {
            var gridRow = (UIElement) AllUsersGrid.ItemContainerGenerator.ContainerFromItem( ViewModel.SelectedUser );
            var gridCell = gridRow?.EnumerateChildren( recursive: true ).OfType<DataGridCell>().FirstOrDefault();
            gridCell?.Focus();
        }

        private void SaveCharts_Click( object sender, RoutedEventArgs e )
        {
            var directoryPath = ViewModel.GetChartsSaveDirectory();
            if ( directoryPath != null )
            {
                if ( !Directory.Exists( directoryPath ) )
                    Directory.CreateDirectory( directoryPath );

                foreach ( var view in ChartsList.EnumerateChildren(true).OfType<ChartTemplateView>() )
                {
                    var rtb = new RenderTargetBitmap( (int) view.ActualWidth, (int) view.ActualHeight, 96, 96, PixelFormats.Pbgra32 );
                    rtb.Render( view );
                    var png = new PngBitmapEncoder { Frames = { BitmapFrame.Create( rtb ) } };

                    using (var stream = File.OpenWrite(Path.Combine(directoryPath, "img" + DateTime.Now.Ticks + ".png")))
                    {
                        png.Save(stream);
                    }
                }
            }
        }
    }
}
