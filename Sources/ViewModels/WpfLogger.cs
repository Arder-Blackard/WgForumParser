using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommonLib.Logging;

namespace ForumParser.ViewModels
{
    public class WpfLogger : ILogger
    {
        #region Auto-properties

        public ObservableCollection<LogEntryBase> Entries { get; set; } = new ObservableCollection<LogEntryBase>();

        public LogSeverity LogLevel { get; set; }

        #endregion


        #region Public methods

        public void Log( LogEntryBase entry ) => Application.Current?.Dispatcher.Invoke( () => Entries.Add( entry ), DispatcherPriority.Send );

        #endregion
    }
}
