using System;
using System.IO;
using CommonLib.Logging;
using Newtonsoft.Json;

namespace ForumParser.Services
{
    public class SettingsManager : ISingletonService
    {
        #region Constants

        private const string SettingsDirectoryName = "ForumParser";
        private const string SettingsFileName = "settings.json";

        #endregion


        #region Fields

        private readonly ILogger _logger;

        #endregion


        #region Auto-properties

        public Settings Settings { get; private set; }

        #endregion


        #region Initialization

        public SettingsManager( ILogger logger )
        {
            _logger = logger;
        }

        #endregion


        #region Public methods

        public void Save()
        {
            var appDirectory = GetAppDirectory();
            if ( !Directory.Exists( appDirectory ) )
                Directory.CreateDirectory( appDirectory );

            var settings = JsonConvert.SerializeObject( Settings, Formatting.Indented,
                                                        new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All } );
            File.WriteAllText( Path.Combine( appDirectory, SettingsFileName ), settings );
        }

        public void Load()
        {
            var settingsPath = Path.Combine( GetAppDirectory(), SettingsFileName );
            try
            {
                _logger?.Trace( () => $"Загрузка настроек из '{settingsPath}'" );

                Settings = JsonConvert.DeserializeObject<Settings>( File.ReadAllText( settingsPath ) );
            }
            catch ( Exception )
            {
                _logger?.Warning( () => $"Не найден файл настроек. Создан файл с настройками по умолчанию '{settingsPath}'" );

                Settings = new Settings();
                Save();
            }
        }

        #endregion


        #region Non-public methods

        private static string GetAppDirectory() => Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), SettingsDirectoryName );

        #endregion
    }

    public class Settings
    {
        #region Auto-properties

        public string Email { get; set; }

        #endregion
    }
}
