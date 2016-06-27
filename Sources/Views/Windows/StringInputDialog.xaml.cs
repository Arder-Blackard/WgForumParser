using System.Windows;
using ForumParser.ViewModels.Windows;
using WpfCommon.Views.Base;

namespace ForumParser.Views.Windows
{
    /// <summary>
    ///     Interaction logic for StringInputDialog.xaml
    /// </summary>
    public partial class StringInputDialog : WindowBase<StringInputDialogViewModel>
    {
        #region Initialization

        public StringInputDialog( StringInputDialogViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }

        #endregion


        #region Event handlers

        private void OkButton_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
            Close();
        }

        private void StringInputDialog_Loaded( object sender, RoutedEventArgs e )
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            if ( Owner == null )
                return;

            Top = ((Window) Owner).Top + (((Window) Owner).Height - Height)/2;
            Left = ((Window) Owner).Left + (((Window) Owner).Width - Width)/2;
        }

        #endregion
    }
}
