using System;
using System.Collections.Generic;
using System.Linq;
using ForumParser.Models;

namespace ForumParser.ViewModels.Controls
{
    /// <summary>
    ///     Describes groupped charts template.
    /// </summary>
    public class EditableChartTemplateViewModel : ChartTemplateViewModel
    {
        #region Properties

        /// <summary>
        ///     Chart width.
        /// </summary>
        public override double Width
        {
            get { return Template.Width; }
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if ( Template.Width == value )
                    return;
                Template.Width = Math.Floor( value );
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Chart height.
        /// </summary>
        public override double Height
        {
            get { return Template.Height; }
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if ( Template.Height == value )
                    return;
                Template.Height = Math.Floor(value);
                OnPropertyChanged();
            }
        }

        #endregion


        #region Initialization

        public EditableChartTemplateViewModel( int answersCount )
            : base( new ChartTemplate { Width = 480, Height = 360, CustomAnswers = new string[answersCount] } )
        {
        }

        public EditableChartTemplateViewModel( ChartTemplate template, IEnumerable<KeyValuePair<TemplateQuestion, PollQuestion>> questions ) : base(template, questions)
        {
        }

        #endregion


        #region Public methods

        /// <summary>
        ///     Adds a question to the template.
        /// </summary>
        /// <param name="question">THe question to add.</param>
        /// <exception cref="ArgumentException">The question answers count doen't match that of the group.</exception>
        public void AddQuestion( PollQuestion question )
        {
            if ( question.Answers.Count != Template.CustomAnswers.Count )
                throw new ArgumentException( $"Cannot add question with {question.Answers.Count} answers to a group with {AnswersCount} answers" );

            if ( Series.Any( series => series.TemplateQuestion.QuestionText == question.Text ) )
                return;

            var templateQuestion = new TemplateQuestion( question );
            Template.Questions.Add( templateQuestion );

            Series.Add( new QuestionSeriesViewModel( templateQuestion, question ) );
            RebuildChart();
        }

        /// <summary>
        ///     Checks whether the template accepts the proposed question.
        /// </summary>
        /// <param name="question">The question to check.</param>
        /// <returns>True if the question can be added to the group.</returns>
        public bool AcceptsQuestion( PollQuestion question )
        {
            return question.Answers.Count == AnswersCount && Series.All( s => s.TemplateQuestion.QuestionText != question.Text );
        }

        /// <summary>
        ///     Removes the <paramref name="question" /> from the template.
        /// </summary>
        /// <param name="question">The question to be removed.</param>
        public void RemoveQuestion( PollQuestion question )
        {
            var seriesIndex =
                Series.Select( ( series, index ) => new { Index = index, series.TemplateQuestion.QuestionText } ).FirstOrDefault( a => a.QuestionText == question.Text )?.Index;

            if ( seriesIndex != null )
                Series.RemoveAt( (int) seriesIndex );

            if ( Series.Count > 0 )
                RebuildChart();
        }

        #endregion

    }

}
