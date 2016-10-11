using ForumParser.ViewModels.Controls;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Windows
{
    public class TemplatePropertiesEditorViewModel : SimpleWindowViewModelBase
    {
        #region Fields

        private TemplateViewModel _template;

        #endregion


        #region Properties

        public TemplateViewModel Template
        {
            get { return _template; }
            set { SetValue( ref _template, value ); }
        }

        #endregion
    }
}
