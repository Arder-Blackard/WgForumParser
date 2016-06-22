﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Services.Default;
using CommonLib.Extensions;
using CommonLib.Logging;
using CommonLib.Progress;
using ForumParserWPF.Models;

namespace ForumParserWPF.Services
{
    public class ParseOptions
    {
        #region Auto-properties

        public bool SkipAdministrators { get; set; }
        public bool SkipCoordinators { get; set; }
        public bool SkipDeleted { get; set; }
        public bool PollOnly { get; set; }

        #endregion
    }

    public class ParserSettings
    {
        #region Auto-properties

        public ICollection<string> Administrators { get; set; } = new List<string> { "Администратор", "Разработчики" };
        public IEnumerable<string> Coordinators { get; set; } = new List<string> { "Координаторы" };

        #endregion
    }

    public sealed class ForumParser
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
            return new ForumParserInternal( topicUrl, sessionId, settings, options, progress, logger, cancellationToken ).Parse( topicUrl );
        }

        #endregion


        #region Nested type: ForumParserInternal

        private class ForumParserInternal
        {
            #region Fields

            private readonly IBrowsingContext _browsingContext;

            private readonly CancellationToken _cancellationToken;
            private readonly ILogger _logger;
            private readonly ParseOptions _options;
            private readonly IProgress<Completion> _progress;
            private readonly ParserSettings _settings;
            private readonly string _topicUrl;

            private readonly Dictionary<string, User> _users = new Dictionary<string, User>();
            private readonly Regex PollAnswerVotesCount = new Regex( @".*?(?<value>\d+)\s+vote", RegexOptions.IgnoreCase );

            private readonly Regex PollAnswerVotesUserIds = new Regex( @"<a\s+href='(?<link>.*?)'>.*<span.*?>(?<name>.*?)<\/span>.*<\/a>",
                                                                       RegexOptions.IgnoreCase | RegexOptions.Multiline );

            private readonly Regex UserIdRegex = new Regex( @".*?(?<id>\d+)/", RegexOptions.IgnoreCase );

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
                _browsingContext = CreateBrowsingContext( topicUrl, sessionId );
            }

            #endregion


            #region Public methods

            public async Task<ForumTopic> Parse( string topicUrl )
            {
                var forumTopic = new ForumTopic();

                if ( _options.PollOnly )
                {
                    var document = await _browsingContext.OpenAsync( topicUrl );
                    forumTopic.Poll = ParsePoll( document ) ?? ParsePoll( await _browsingContext.OpenAsync( FindPollResultsLink( document ) ) );
                    return forumTopic;
                }
                _logger?.Info( "Загрузка отзывов" );

                var documents = await LoadForumTopicPages( topicUrl );

                forumTopic.Name = documents.Select( ParseTopicName ).FirstOrDefault( name => name != null );

                _logger?.Info( $"Имя темы: {forumTopic.Name ?? "<не определено>"}" );

                _logger?.Info( "Анализ отзывов" );

                documents.ForEach( ParseFeedbacks );

                _logger?.Info( "Анализ голосования" );

                forumTopic.Poll = ParsePoll( documents[0] ) ?? ParsePoll( await _browsingContext.OpenAsync( FindPollResultsLink( documents[0] ) ) );

                _logger?.Info( "Сбор данных о пользователях" );

                forumTopic.Users = _users.Values.OrderBy( user => user.Name ).ToList();

                _logger?.Info( "Загрузка данных завершена" );

                return forumTopic;
            }

            #endregion


            #region Non-public methods

            private string ParseTopicName( IDocument document )
            {
                return document.QuerySelector( "h1.ipsType_pagetitle" ).TextContent.Trim();
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

            private void ParsePost( IElement post )
            {
                var profileLink = post.QuerySelector( "a[hovercard-ref=member]" )?.Attributes["href"]?.Value;
                if ( profileLink == null )
                    return;

                var userId = ParseUserIdFromProfileLink( profileLink );
                if ( userId == null )
                    return;

                var user = _users.GetOrInsert( userId, () => new User { Id = userId } );

                user.Group = post.QuerySelector( "li.group_title span" )?.TextContent ?? string.Empty;
                user.Name = post.QuerySelector( "span[itemprop=name]" )?.TextContent ?? string.Empty;

                if ( _options.SkipAdministrators && _settings.Administrators.Contains( user.Group ) ||
                     _options.SkipCoordinators && _settings.Coordinators.Contains( user.Group ) )
                    return;

                foreach ( var postBlock in post.QuerySelectorAll( "div.post_body" ) )
                {
                    if ( postBlock.Id?.StartsWith( "postsDelete" ) != true )
                    {
                        var commentText = postBlock.QuerySelector( "div[itemprop=commentText]" );
                        if ( string.Equals( commentText?.TextContent.Trim(), "DEL", StringComparison.OrdinalIgnoreCase ) )
                            user.HasDeletedFeedback = true;
                        else
                            user.HasFeedback = true;
                    }
                    else
                        user.HasDeletedPost = true;
                }
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
                        var user = _users.GetOrInsert( userId, () => new User { Id = userId } );
                        user.Name = match.Groups["name"].Value;
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