using ForumParserWPF.ViewModels.Windows;

namespace ForumParserWPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ViewBase<MainWindowViewModel>
    {
        public MainWindow( MainWindowViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }
    }
}
