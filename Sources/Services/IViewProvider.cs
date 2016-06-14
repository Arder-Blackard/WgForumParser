using System;
using ForumParserWPF.ViewModels;

namespace ForumParserWPF.Services
{
    public interface IViewResult<out TViewModel> where TViewModel : class, IViewModel
    {
        TViewModel ViewModel { get; }
        bool? Result { get; }
    }

    public interface IViewProvider
    {
        IViewResult<TViewModel> Show<TViewModel>( IViewModel ownerViewModel = null, Action<TViewModel> initializer = null ) where TViewModel : class, IViewModel;
    }
}