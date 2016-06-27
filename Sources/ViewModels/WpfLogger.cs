using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommonLib.Logging;

namespace ForumParser.ViewModels
{
    public class WpfLogger : ILogger
    {
        private readonly DefaultLogger _fileLogger;


        #region Auto-properties

        public ObservableCollection<LogEntryBase> Entries { get; } = new ObservableCollection<LogEntryBase>();

        public LogSeverity LogLevel { get; set; }

        #endregion


        #region Public methods

        public void Log( LogEntryBase entry )
        {
            _fileLogger.Log( entry );
            Application.Current?.Dispatcher.Invoke( () => Entries.Add( entry ), DispatcherPriority.Send );
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public WpfLogger()
        {
            _fileLogger = new DefaultLogger( "ForumParser.log", "ForumParser", LogSeverity.Debug );
        }
    }
}
