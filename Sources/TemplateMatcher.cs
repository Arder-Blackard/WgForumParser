using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Extensions;
using ForumParser.Models;
using ForumParser.Services;

namespace ForumParser
{
    public class TemplateMatcher : ISingletonService
    {
        #region Public methods

        public IEnumerable<KeyValuePair<ChartTemplate, List<KeyValuePair<TemplateQuestion, PollQuestion>>>> MatchTemplates(
            IEnumerable<ChartTemplate> templates,
            IEnumerable<PollQuestion> questions )
        {
            var questionsMap = questions.ToDictionary( question => question.Text, StringComparer.OrdinalIgnoreCase );

            return from template in templates
                   let matchingQuestions = template.Questions
                                                   .Select( q => MakeKeyValuePair( q, FindMatchingPollQuestion( q, questionsMap ) ) )
                                                   .Where( pair => pair.Value != null )
                                                   .ToList()
                   where matchingQuestions.Any()
                   select MakeKeyValuePair( template, matchingQuestions );
        }

        #endregion


        #region Non-public methods

        private static KeyValuePair<TKey, TValue> MakeKeyValuePair<TKey, TValue>( TKey key, TValue value ) => new KeyValuePair<TKey, TValue>( key, value );

        private static PollQuestion FindMatchingPollQuestion( TemplateQuestion templateQuestion, Dictionary<string, PollQuestion> questionsMap )
        {
            var pollQuestion = questionsMap.GetOrDefault( templateQuestion.QuestionText );

            //  Determine cases when match is unsuccessful
            if ( pollQuestion == null )
                return null;

            if ( pollQuestion.Answers.Count != templateQuestion.Answers.Count )
                return null;

            if ( !pollQuestion.Answers
                              .Zip( templateQuestion.Answers,
                                    ( pollAnswer, templateAnswerText ) => pollAnswer.Text.Equals( templateAnswerText, StringComparison.OrdinalIgnoreCase ) )
                              .All( match => match ) )
                return null;

            //  The matching poll question has been found
            return pollQuestion;
        }

        #endregion
    }
}
