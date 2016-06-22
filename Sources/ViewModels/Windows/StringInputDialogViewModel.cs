using WpfCommon.ViewModels.Base;

namespace ForumParserWPF.ViewModels.Windows
{
    public class StringInputDialogViewModel : SimpleWindowViewModelBase
    {
        #region Auto-properties

        public string ViewTitle { get; set; }

        public string Description { get; set; }

        public string StringInput { get; set; }

        #endregion
    }
}
