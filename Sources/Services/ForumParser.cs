using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Services.Default;
using CommonLib.Extensions;
using CommonLib.Logging;
using CommonLib.Progress;
using ForumParser.Exceptions;
using ForumParser.Models;

namespace ForumParser.Services
{
    public class ParseOptions
    {
        #region Auto-properties

        public bool ExcludeAdministrators { get; set; }
        public bool ExcludeCoordinators { get; set; }
        public bool ExcludeDeletedMessages { get; set; }
        public bool ParsePollOnly { get; set; }

        #endregion
    }

    public class ParserSettings
    {
        #region Auto-properties

        public ICollection<string> Administrators { get; set; } = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "Администратор", "Разработчики" };
        public ICollection<string> Coordinators { get; set; } = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "Координаторы" };
        public ICollection<string> DeletedPosts { get; set; } = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "del", "delete" };

        #endregion
    }

    public sealed class ForumTopicParser : ISingletonService
    {
        #region Public methods

        public Task<ForumTopic> Parse( string topicUrl,
                                       string sessionId,
                                       ParserSettings settings,
                                       ParseOptions options,
                                       IProgress<Completion> progress = null,
                                       ILogger logger = null,
                                       CancellationToken cancellationToken = default(CancellationToken) )
        {
            return new ForumParserInternal( topicUrl, sessionId, settings, options, progress, logger, cancellationToken ).ParseAsync( topicUrl );
        }

        #endregion


        #region Nested type: ForumParserInternal

        private class ForumParserInternal
        {
            #region Constants

            private static readonly Regex PollAnswerVotesCount = new Regex( @".*?(?<value>\d+)\s+(vote|голос)", RegexOptions.IgnoreCase );

            private static readonly Regex PollAnswerVotesUserIds = new Regex( @"<a\s+href='(?<link>.*?)'>(?:(?:.*<span.*?>(?<name>.*?)<\/span>.*)|(?<name2>.*))<\/a>",
                                                                              RegexOptions.IgnoreCase | RegexOptions.Multiline );

            private static readonly Regex UserIdRegex = new Regex( @".*?(?<id>\d+)/", RegexOptions.IgnoreCase );

            #endregion


            #region Fields

            private readonly IBrowsingContext _browsingContext;

            private readonly CancellationToken _cancellationToken;
            private readonly ILogger _logger;
            private readonly ParseOptions _options;
            private readonly IProgress<Completion> _progress;
            private readonly ParserSettings _settings;
            private readonly string _topicUrl;

            private readonly Dictionary<string, User> _users = new Dictionary<string, User>();
            private readonly List<User> _usersList = new List<User>();

            #endregion


            #region Initialization

            public ForumParserInternal( string topicUrl,
                                        string sessionId,
                                        ParserSettings settings,
                                        ParseOptions options,
                                        IProgress<Completion> progress,
                                        ILogger logger,
                                        CancellationToken cancellationToken )
            {
                _topicUrl = topicUrl;
                _settings = settings;
                _options = options;
                _progress = progress;
                _logger = logger;
                _cancellationToken = cancellationToken;

                try
                {
                    _browsingContext = CreateBrowsingContext( topicUrl, sessionId );
                }
                catch (Exception ex)
                {
                    throw new ForumParserException( "Невозможно подключиться к указанному адресу", ex );
                }
            }

            #endregion


            #region Public methods

            public Task<ForumTopic> ParseAsync( string topicUrl )
            {
                var forumTopic = new ForumTopic { Url = topicUrl };

                return _options.ParsePollOnly
                           ? ParsePollOnlyAsync( topicUrl, forumTopic )
                           : ParseFullTopicAsync( topicUrl, forumTopic );
            }

            #endregion


            #region Non-public methods

            private async Task<ForumTopic> ParseFullTopicAsync( string topicUrl, ForumTopic forumTopic )
            {
                _logger?.Info( "Загрузка отзывов..." );

                var documents = await LoadForumTopicPages( topicUrl );

                forumTopic.Name = documents.Select( ParseTopicName ).FirstOrDefault( name => name != null );

                _logger?.Info( $"Имя темы: {forumTopic.Name ?? "<не определено>"}" );

                _logger?.Info( "Анализ отзывов..." );

                documents.ForEach( ParseFeedbacks );

                _logger?.Info( "Анализ голосования..." );

                forumTopic.Poll = ParsePoll( documents[0] );
                if ( forumTopic.Poll == null )
                {
                    var pollResultsLink = FindPollResultsLink( documents[0] );
                    forumTopic.Poll = pollResultsLink != null ? ParsePoll( await _browsingContext.OpenAsync( pollResultsLink ) ) : new Poll();
                }

                _logger?.Info( "Сбор данных о пользователях..." );

                forumTopic.Users = _usersList;

                if ( forumTopic.Users.Count == 0 && forumTopic.Poll.Questions.Count == 0 )
                    _logger?.Info( "Не найдены данные, соответствующие формату темы форума" );
                else
                    _logger?.Info( "Загрузка данных завершена" );

                return forumTopic;
            }

            private async Task<ForumTopic> ParsePollOnlyAsync( string topicUrl, ForumTopic forumTopic )
            {
                _logger?.Info( "Загрузка голосования..." );

                var document = await _browsingContext.OpenAsync( topicUrl );


                try
                {
                    using (var stream = File.OpenWrite("r:\\p.txt"))
                    using ( var writer = new StreamWriter( stream ) )
                        document.ToHtml( writer, new PrettyMarkupFormatter() );
                }
                catch
                {
                }


                forumTopic.Poll = ParsePoll(document);
                if (forumTopic.Poll == null)
                {
                    var pollResultsLink = FindPollResultsLink(document);
                    forumTopic.Poll = pollResultsLink != null ? ParsePoll(await _browsingContext.OpenAsync(pollResultsLink)) : new Poll();
                }

                _logger?.Info( "Сбор данных о пользователях..." );

                forumTopic.Users = _usersList;

                if (forumTopic.Users.Count == 0 && forumTopic.Poll.Questions.Count == 0)
                    _logger?.Info("Не найдены данные, соответствующие формату темы форума");
                else
                    _logger?.Info("Загрузка данных завершена");


                return forumTopic;
            }

            /// <summary>
            ///     Reads topic name from the block.
            /// </summary>
            /// <param name="document"></param>
            /// <returns></returns>
            private string ParseTopicName( IDocument document )
            {
                return document.QuerySelector( "h1.ipsType_pagetitle" )?.TextContent.Trim();
            }

            /// <summary>
            ///     Returns link to the poll results page.
            /// </summary>
            /// <param name="document"></param>
            /// <returns></returns>
            private static string FindPollResultsLink( IDocument document )
            {
                return document.QuerySelector( "a#poll_showresults" )?.Attributes["href"]?.Value;
            }

            private void ParseFeedbacks( IDocument document )
            {
                var posts = document.QuerySelectorAll( "div.ipsBox>div#ips_Posts>div[id^=post_id_]" );
                foreach ( var post in posts )
                    ParsePost( post );
            }

            /// <summary>
            ///     Parses the post contained in <paramref name="post" /> element.
            /// </summary>
            /// <param name="post">The post element.</param>
            private void ParsePost( IElement post )
            {
                var profileLink = post.QuerySelector( "a[hovercard-ref=member]" )?.Attributes["href"]?.Value;
                if ( profileLink == null )
                    return;

                var userId = ParseUserIdFromProfileLink( profileLink );
                if ( userId == null )
                    return;

                //  Get and check the name and the group of the user
                var userGroup = post.QuerySelector("li.group_title span")?.TextContent ?? string.Empty;
                var userName = post.QuerySelector("span[itemprop=name]")?.TextContent ?? string.Empty;

                if ( _options.ExcludeAdministrators && _settings.Administrators.Contains( userGroup ) )
                    return;

                if ( _options.ExcludeCoordinators && _settings.Coordinators.Contains( userGroup ) )
                    return;

                //  Get and check the post content
                var postBlock = post.QuerySelector( "div.post_body" );
                var commentText = postBlock?.QuerySelector( "div[itemprop=commentText]" );

                var isPostDeleted = postBlock?.Id?.StartsWith( "postsDelete", StringComparison.Ordinal ) == true;
                var isFeedbackDeleted = !isPostDeleted && _settings.DeletedPosts.Contains( commentText?.TextContent.Trim() );
                var userHasFeedback = postBlock != null && !isFeedbackDeleted;

                if ( _options.ExcludeDeletedMessages && (isPostDeleted || isFeedbackDeleted) )
                    return;

                //  Create or update the user
                var user = GetOrInsertUser( userId, () => new User { Id = userId, Name = userName, Group = userGroup, } );

                user.HasFeedback = userHasFeedback;
                user.HasDeletedFeedback = isFeedbackDeleted;
                user.HasDeletedPost = isPostDeleted;

                if ( commentText != null )
                    user.Messages.Add( new Message { IsDeleted = isFeedbackDeleted, Html = commentText.OuterHtml } );
            }

            private User GetOrInsertUser( string userId, Func<User> generator )
            {
                var user = _users.GetOrDefault( userId );
                if ( user == null )
                {
                    user = generator();
                    _users[userId] = user;
                    _usersList.Add( user );
                }
                return user;
            }

            private string ParseUserIdFromProfileLink( string profileLink )
            {
                var match = UserIdRegex.Match( profileLink );
                if ( !match.Success )
                    return null;

                var userId = match.Groups["id"].Value;
                return userId;
            }

            private async Task<List<IDocument>> LoadForumTopicPages( string topicUrl )
            {
                var pages = new List<IDocument>();
                var nextUrl = topicUrl;
                do
                {
                    _logger?.Info( "Загрузка страницы " + Uri.UnescapeDataString( nextUrl ) );

                    var document = await _browsingContext.OpenAsync( nextUrl );

                    try
                    {
                        using ( var stream = File.OpenWrite( "r:\\t" + nextUrl.Split( '\\', '/' ).Last() + ".txt" ) )
                        using ( var writer = new StreamWriter( stream ) )
                            document.ToHtml( writer, new PrettyMarkupFormatter() );
                    }
                    catch
                    {
                    }

                    pages.Add( document );
                    nextUrl = document.QuerySelector( "html>head>link[rel=next]" )?.Attributes["href"].Value;
                } while ( !string.IsNullOrEmpty( nextUrl ) );
                return pages;
            }

            private Poll ParsePoll( IDocument document )
            {
                var pollQuestionsVoted = document.QuerySelectorAll( "form[name=pollForm]>div.poll_question.voted" ).ToList();
                if ( pollQuestionsVoted.Count == 0 )
                    return null;

                return new Poll { Questions = pollQuestionsVoted.Select( ParseQuestion ).Where( q => q != null ).ToList() };
            }

            private PollQuestion ParseQuestion( IElement div )
            {
                try
                {
                    var text = div.QuerySelector( "h4" ).TextContent;
                    if ( text == null )
                    {
                        _logger?.Error( "Не найден текст вопроса голосования." );
                        return null;
                    }

                    return new PollQuestion
                    {
                        Text = text,
                        Answers = div.QuerySelectorAll( "ol>li" ).Select( ParseAnswer ).ToList()
                    };
                }
                catch ( Exception ex ) when ( _logger != null )
                {
                    _logger.Error( $"Ошибка при разборе вопроса голосования: {ex.Message}", ex );
                    return null;
                }
            }

            private PollAnswerBase ParseAnswer( IElement answer )
            {
                var answerText = answer.QuerySelector( "span.answer" ).TextContent;
                if ( answerText == null )
                    throw new InvalidDataException( "Не найден текст варианта ответа в голосовании" );

                var popupScript = answer.QuerySelector( "script" );
                if ( popupScript != null )
                {
                    var users = new List<User>();

                    foreach ( Match match in PollAnswerVotesUserIds.Matches( popupScript.TextContent ) )
                    {
                        var userId = ParseUserIdFromProfileLink( match.Groups["link"].Value );
                        var user = GetOrInsertUser( userId, () => new User { Id = userId } );
                        user.Name = match.Groups["name"].Success ? match.Groups["name"].Value : match.Groups["name2"].Value;
                        user.HasVote = true;

                        users.Add( user );
                    }

                    return new PollAnswer( answerText, users );
                }

                var votes = answer.QuerySelector( "span.votes" )?.TextContent;
                if ( votes == null )
                    throw new InvalidDataException( $"Не найдена информация о количестве голосов в ответе '{answerText}'" );

                var votesCountMatch = PollAnswerVotesCount.Match( votes );
                if ( !votesCountMatch.Success )
                    throw new InvalidDataException( $"Не найдена информация о количестве голосов в ответе '{answerText}'" );

                return new AnonymousPollAnswer( answerText, votesCountMatch.Groups["value"].Value.To<int>() );
            }

            private static IBrowsingContext CreateBrowsingContext( string topicUrl, string sessionId )
            {
                var url = Url.Create( topicUrl );
                var config = new Configuration().WithDefaultLoader().With( new MemoryCookieService { [url.Origin] = $"frm_session_id={sessionId}" } );
                return BrowsingContext.New( config );
            }

            #endregion
        }

        #endregion
    }
}
