using System.Windows;
using ForumParserWPF.ViewModels.Windows;
using WpfCommon.Views.Base;

namespace ForumParserWPF.Views.Windows
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
