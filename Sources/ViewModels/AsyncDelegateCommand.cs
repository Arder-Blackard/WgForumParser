using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ForumParserWPF.ViewModels
{
    /// <summary>
    ///     AsyncDelegateCommand from here
    ///     http://blog.mycupof.net/2012/08/23/mvvm-asyncdelegatecommand-what-asyncawait-can-do-for-uidevelopment/.
    /// </summary>
    public class AsyncDelegateCommand : ICommand
    {
        #region Fields

        protected readonly Func<object, Task> asyncExecute;

        protected readonly Predicate<object> canExecute;

        #endregion


        #region Events and invocation

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion


        #region Initialization

        public AsyncDelegateCommand( Func<object, Task> asyncExecute, Predicate<object> canExecute = null )
        {
            this.asyncExecute = asyncExecute;
            this.canExecute = canExecute;
        }

        #endregion


        #region Public methods

        public bool CanExecute( object parameter ) => canExecute == null || canExecute( parameter );

        public async void Execute( object parameter ) => await ExecuteAsync( parameter );

        #endregion


        #region Non-public methods

        protected virtual async Task ExecuteAsync( object parameter ) => await asyncExecute( parameter );

        #endregion
    }
}
