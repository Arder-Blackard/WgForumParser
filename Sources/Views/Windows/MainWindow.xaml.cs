using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ForumParser.ViewModels.Windows;
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
    }
}
