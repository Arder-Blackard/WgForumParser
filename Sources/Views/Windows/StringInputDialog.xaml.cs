using System.Windows;
using ForumParser.ViewModels.Windows;
using WpfCommon.Views.Base;

namespace ForumParser.Views.Windows
{
    /// <summary>
    /// Interaction logic for StringInputDialog.xaml
    /// </summary>
    public partial class StringInputDialog : WindowBase<StringInputDialogViewModel>
    {
        public StringInputDialog( StringInputDialogViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }

        private void OkButton_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
            Close();
        }
    }
}
