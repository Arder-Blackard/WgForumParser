using System;
using System.Collections.Generic;
using System.Windows;
using CommonLib.Extensions;
using ForumParserWPF.Services;
using ForumParserWPF.ViewModels;
using ForumParserWPF.Views;
using SimpleInjector;

namespace ForumParserWPF.CompositionRoot
{
    public class ViewProvider : IViewProvider
    {
        #region Fields

        private readonly Container _container;

        private readonly Dictionary<IViewModel, Window> _openViews = new Dictionary<IViewModel, Window>();

        #endregion


        #region Initialization

        public ViewProvider( Container container )
        {
            _container = container;
        }

        #endregion


        #region Public methods

        public IViewResult<TViewModel> Show<TViewModel>( IViewModel ownerViewModel = null, Action<TViewModel> initializer = null ) where TViewModel : class, IViewModel
        {
            var view = CreateWindow<TViewModel>();

            initializer?.Invoke( view.ViewModel );

            if ( ownerViewModel != null )
                view.Owner = _openViews.GetOrDefault( ownerViewModel );

            var dialogResult = view.ShowDialog();

            return new ViewResult<TViewModel>( view.ViewModel, dialogResult );
        }

        public ViewBase<TViewModel> CreateWindow<TViewModel>() where TViewModel : class, IViewModel
        {
            var viewType = typeof( ViewBase<TViewModel> );

            //  Instantiate the view and store it in the map of views open
            var view = (ViewBase<TViewModel>) _container.GetInstance( viewType );
            var viewModel = view.ViewModel;

            _openViews[viewModel] = view;
            view.Closed += ( sender, args ) => _openViews.Remove( viewModel );

            return view;
        }

        #endregion
    }

    public class ViewResult<TViewModel> : IViewResult<TViewModel> where TViewModel : class, IViewModel
    {
        #region Auto-properties

        public TViewModel ViewModel { get; }
        public bool? Result { get; }

        #endregion


        #region Initialization

        public ViewResult( TViewModel viewModel, bool? result )
        {
            ViewModel = viewModel;
            Result = result;
        }

        #endregion
    }
}
