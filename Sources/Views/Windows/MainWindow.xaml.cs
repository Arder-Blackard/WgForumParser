using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForumParserWPF.ViewModels.Windows;
using ForumParserWPF.Views.Extensions;
using WpfCommon.Views.Base;

namespace ForumParserWPF.Views.Windows
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
            if ( e.IsDown && e.Key == Key.Delete )
            {
                var focused = FocusManager.GetFocusedElement(this);

                ViewModel.DeleteSelectedUserCommand?.Execute( null );

                focused = FocusManager.GetFocusedElement( this );

                UpdateLayout();

                focused = FocusManager.GetFocusedElement( this );

                UsersGrid.Focus();

                focused = FocusManager.GetFocusedElement(this);

                var gridRow = ((UIElement) UsersGrid.ItemContainerGenerator.ContainerFromItem( ViewModel.SelectedUser ));
                var gridCell = gridRow.EnumerateChildren( recursive: true ).OfType<DataGridCell>().FirstOrDefault();
                gridCell?.Focus();

                focused = FocusManager.GetFocusedElement(this);
            }
        }
    }
}
