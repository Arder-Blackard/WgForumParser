using ForumParser.ViewModels.Windows;
using WpfCommon.Views.Base;

namespace ForumParser.Sources.Views.Windows
{
    /// <summary>
    ///     Interaction logic for TemplatePropertiesEditor.xaml
    /// </summary>
    public partial class TemplatePropertiesEditor : WindowBase<TemplatePropertiesEditorViewModel>
    {
        #region Initialization

        public TemplatePropertiesEditor( TemplatePropertiesEditorViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }

        #endregion
    }
}
