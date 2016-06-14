using System;
using System.Windows;
using CommonLib.Logging;
using ForumParserWPF.CompositionRoot;
using ForumParserWPF.Properties;
using ForumParserWPF.Services;
using ForumParserWPF.ViewModels.Windows;
using ForumParserWPF.Views;
using SimpleInjector;

namespace ForumParserWPF
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
            ComposeObjects( container );
            Current.MainWindow.Show();
        }

        #endregion


        #region Non-public methods

        private void ComposeObjects( Container container )
        {
            var viewProvider = (ViewProvider) container.GetInstance<IViewProvider>();
            Current.MainWindow = viewProvider.CreateWindow<MainWindowViewModel>();
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private static Container Bootstrap()
        {
            var assembly = typeof (App).Assembly;

            // IoC container.
            var container = new Container();

            //  View locator
            container.RegisterSingleton<IViewProvider>( () => new ViewProvider( container ) );

            //  Settings
            container.RegisterSingleton( () => Settings.Default );

            //  Common services
            container.RegisterSingleton<ILogger>( () => new DefaultLogger( "ForumParser.log", $"ForumParser v.{assembly.GetName().Version}" ) );
            container.RegisterSingleton<CookieService>();
            container.RegisterSingleton<ForumParser>();

            //  ViewModels
            container.Register<MainWindowViewModel>();

            //  Windows

            container.Register( typeof (ViewBase<>), new[] { assembly } );

            container.Verify();

            return container;
        }

        #endregion
    }
}
