using ForumParserWPF.Models;
using WpfCommon.ViewModels.Base;

namespace ForumParserWPF.ViewModels.Windows
{
    public class TemplateEditorViewModel : DependencyObjectViewModelBase, IViewModel
    {
        #region Fields

        private ForumTopic _forumTopic;

        #endregion


        #region Properties

        public string ViewTitle => "Редактор шаблонов";

        public ForumTopic ForumTopic
        {
            get { return _forumTopic; }
            set { SetValue( ref _forumTopic, value ); }
        }

        #endregion
    }
}
