using ForumParserWPF.ViewModels.Windows;

namespace ForumParserWPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TemplateEditor : ViewBase<TemplateEditorViewModel>
    {
        public TemplateEditor( TemplateEditorViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }
    }
}
