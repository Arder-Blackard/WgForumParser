using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommonLib.Logging;
using ForumParser.Properties;
using ForumParser.Services;
using ForumParser.ViewModels;
using ForumParser.ViewModels.Windows;
using SimpleInjector;
using WpfCommon.Services;
using WpfCommon.ViewModels.Base;
using WpfCommon.Views.Base;

namespace ForumParser
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        #region Events and invocation

        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );
            var container = Bootstrap();

            container.GetInstance<SettingsManager>().Load();

            ComposeObjects( container );
            Current.MainWindow.Show();
        }

        #endregion


        #region Non-public methods

        private void ComposeObjects( Container container )
        {
            var viewProvider = (ViewProvider) container.GetInstance<IViewProvider>();
            Current.MainWindow = (Window)viewProvider.CreateWindow<MainWindowViewModel>();
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private static Container Bootstrap()
        {
            var assembly = typeof (App).Assembly;

            // IoC container.
            var container = new Container();
            container.RegisterSingleton<IServiceProvider>( container );

            //  View locator
            container.RegisterSingleton<IViewProvider, ViewProvider>();


            //  Common services
            container.RegisterSingleton<ILogger, WpfLogger>();
            foreach ( var type in typeof ( App ).Assembly.ExportedTypes.Where( type => !type.IsInterface && typeof ( ISingletonService ).IsAssignableFrom( type ) ).ToList() )
                container.RegisterSingleton( type, type );

            //  ViewModels
            container.Register<MainWindowViewModel>();
            container.Register<LoginHelperWindowViewModel>();
            container.Register<TemplateEditorViewModel>();
            container.Register<StringInputDialogViewModel>();

            var wpfCommonAssembly = typeof(WindowBase<>).Assembly;

            //  ViewModels
            container.RegisterCollection(typeof(IWindowViewModel), assembly, wpfCommonAssembly);

            //  Windows
            container.Register(typeof(IView<>), new[] { assembly, wpfCommonAssembly });


            container.Verify();

            return container;
        }

        #endregion
    }
}
