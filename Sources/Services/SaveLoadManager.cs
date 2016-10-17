using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CommonLib.Logging;
using ForumParser.Exceptions;
using ForumParser.Models;
using Newtonsoft.Json;
using WpfCommon.Services;

namespace ForumParser.Services
{
    public class SaveLoadManager : ISingletonService
    {
        #region Constants

        private static readonly JsonSerializerSettings JsonPreserveLinks = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All
        };

        private static readonly JsonSerializerSettings JsonDefault = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        #endregion


        #region Fields

        private readonly ILogger _logger;

        #endregion


        #region Initialization

        public SaveLoadManager( IViewProvider viewProvider, ILogger logger )
        {
            _logger = logger;
        }

        #endregion


        #region Public methods

        public void SaveChartTemplates( IEnumerable<ChartTemplate> templates, string chartTemplatesFile )
        {
            var json = JsonConvert.SerializeObject( templates.ToList(), Formatting.Indented, JsonDefault );

            if ( File.Exists( chartTemplatesFile ) )
                File.Delete( chartTemplatesFile );

            using ( var file = ZipFile.Open( chartTemplatesFile, ZipArchiveMode.Create ) )
            using ( var stream = file.CreateEntry( "templates.json" ).Open() )
            using ( var writer = new StreamWriter( stream ) )
                writer.Write( json );
        }

        public void SaveIntermediateResults( ForumTopic forumTopic, string forumTopicFile )
        {
            var json = JsonConvert.SerializeObject( forumTopic, Formatting.Indented, JsonPreserveLinks );

            if ( File.Exists( forumTopicFile ) )
                File.Delete( forumTopicFile );

            using ( var file = ZipFile.Open( forumTopicFile, ZipArchiveMode.Create ) )
            using ( var stream = file.CreateEntry( "topic.json" ).Open() )
            using ( var writer = new StreamWriter( stream ) )
                writer.Write( json );
        }

        public ForumTopic LoadIntermediateResults( string forumTopicFile )
        {
            try
            {
                using ( var zipArchive = ZipFile.Open( forumTopicFile, ZipArchiveMode.Read ) )
                using ( var stream = zipArchive.GetEntry( "topic.json" )?.Open() )
                {
                    if ( stream == null )
                        throw new IOException( "Неверный формат сохраненных данных." );

                    using ( var reader = new StreamReader( stream ) )
                    {
                        var json = reader.ReadToEnd();
                        try
                        {
                            return JsonConvert.DeserializeObject<ForumTopic>( json, JsonPreserveLinks );
                        }
                        catch ( Exception ex )
                        {
                            throw new InvalidDataException( "Ошибка при чтении 'topic.json'.", ex );
                        }
                    }
                }
            }
            catch ( FileNotFoundException ex )
            {
                throw new ForumParserException( $"Файл не найден.", ex );
            }
            catch ( IOException ex )
            {
                throw new ForumParserException( $"Ошибка при загрузке файла.", ex );
            }
            catch ( InvalidDataException ex )
            {
                throw new ForumParserException( $"Неверный формат данных.", ex );
            }
            catch ( Exception ex )
            {
                throw new ForumParserException( $"Ошибка при загрузке: ", ex );
            }
        }

        public ICollection<ChartTemplate> LoadChartTemplates( string chartTemplatesFile )
        {
            try
            {
                using ( var zipArchive = ZipFile.Open( chartTemplatesFile, ZipArchiveMode.Read ) )
                using ( var stream = zipArchive.GetEntry( "templates.json" )?.Open() )
                {
                    if ( stream == null )
                        throw new IOException( "Неверный формат сохраненных данных." );

                    using ( var reader = new StreamReader( stream ) )
                    {
                        var json = reader.ReadToEnd();
                        try
                        {
                            return JsonConvert.DeserializeObject<ICollection<ChartTemplate>>( json, JsonDefault );
                        }
                        catch ( Exception ex )
                        {
                            throw new InvalidDataException( "Ошибка при чтении 'topic.json'.", ex );
                        }
                    }
                }
            }
            catch ( FileNotFoundException ex )
            {
                throw new ForumParserException( $"Файл не найден.", ex );
            }
            catch ( IOException ex )
            {
                throw new ForumParserException( $"Ошибка при загрузке файла.", ex );
            }
            catch ( InvalidDataException ex )
            {
                throw new ForumParserException( $"Неверный формат данных.", ex );
            }
            catch ( Exception ex )
            {
                throw new ForumParserException( $"Ошибка при загрузке: ", ex );
            }
        }

        #endregion
    }
}
