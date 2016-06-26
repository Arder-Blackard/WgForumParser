using System.Collections.Generic;
using System.Linq;
using ForumParser.Models;

namespace ForumParser.ViewModels.Controls
{
    public class PollQuestionChartViewModel
    {
        #region Auto-properties

        public ICollection<PollAnswerBar> Answers { get; }

        public PollQuestion Question { get; }

        #endregion


        #region Properties

        public string Text => Question.Text;

        #endregion


        #region Initialization

        public PollQuestionChartViewModel( PollQuestion question )
        {
            Question = question;
            var totalCount = Question.Answers.Sum( answer => answer.Count );
            Answers = question.Answers.Select( answer => new PollAnswerBar( answer.Text, answer.Count, totalCount ) ).ToArray();
        }

        #endregion
    }

    public class PollAnswerBar
    {
        #region Auto-properties

        public string Text { get; }

        public double FilledBarWidth { get; }
        public double EmptyBarWidth { get; }

        #endregion


        #region Initialization

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public PollAnswerBar( string text, int answerCount, int totalCount )
        {
            Text = text;
            FilledBarWidth = (double) answerCount/totalCount;
            EmptyBarWidth = 1.0 - FilledBarWidth;
        }

        #endregion
    }
}
