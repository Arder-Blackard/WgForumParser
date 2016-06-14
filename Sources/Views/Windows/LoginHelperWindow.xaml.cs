using System.Windows;
using ForumParserWPF.ViewModels.Windows;

namespace ForumParserWPF.Views.Windows
{
    /// <summary>
    ///     Interaction logic for LoginHelperWindow.xaml
    /// </summary>
    public partial class LoginHelperWindow : ViewBase<LoginHelperWindowViewModel>
    {
        #region Initialization

        public LoginHelperWindow( LoginHelperWindowViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }

        #endregion


        #region Event handlers

        private void Hyperlink_Click( object sender, RoutedEventArgs e )
        {
            WebBrowser.Address = "http://supertest.worldoftanks.com";

            //            WebBrowser.Reload();
        }

        private void ConfirmLogin_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
            Close();
        }

        #endregion
    }
}
