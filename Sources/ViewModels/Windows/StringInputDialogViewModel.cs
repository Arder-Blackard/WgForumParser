using WpfCommon.ViewModels.Base;

namespace ForumParserWPF.ViewModels.Windows
{
    public class StringInputDialogViewModel : SimpleWindowViewModelBase
    {
        #region Fields

        private string _viewTitle;
        private string _description;
        private string _stringInput;

        #endregion


        #region Properties

        public string ViewTitle
        {
            get { return _viewTitle; }
            set { SetValue( ref _viewTitle, value ); }
        }

        public string Description
        {
            get { return _description; }
            set { SetValue( ref _description, value ); }
        }

        public string StringInput
        {
            get { return _stringInput; }
            set { SetValue( ref _stringInput, value ); }
        }

        #endregion
    }
}
