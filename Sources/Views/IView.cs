using ForumParserWPF.ViewModels;
using WgWpfEffects.Components;

namespace ForumParserWPF.Views
{
    public class ViewBase<TViewModel> : WgWindow, IViewBase<TViewModel> where TViewModel : class, IViewModel
    {
        #region Properties

        public TViewModel ViewModel => DataContext as TViewModel;

        #endregion


        #region Initialization

        protected ViewBase( TViewModel viewModel )
        {
            DataContext = viewModel;
        }

        #endregion
    }

    public interface IViewBase<out TViewModel> where TViewModel : class, IViewModel
    {
        #region Properties

        TViewModel ViewModel { get; }

        #endregion
    }
}
