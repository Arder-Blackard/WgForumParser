using System.Collections.Generic;
using System.Linq;

namespace ForumParser.Models
{
    public class ChartTemplate
    {
        #region Auto-properties

        public double Width { get; set; }
        public double Height { get; set; }

        public IList<TemplateQuestion> Questions { get; set; } = new List<TemplateQuestion>();
        public IList<string> CustomAnswers { get; set; } 

        #endregion
    }

    public class TemplateQuestion
    {
        #region Auto-properties

        /// <summary>
        ///     The original text of the question.
        /// </summary>
        public string QuestionText { get; set; }

        /// <summary>
        ///     The text overriden by user.
        /// </summary>
        public string CustomText { get; set; }

        /// <summary>
        ///     Question answers text.
        /// </summary>
        public IList<string> Answers { get; set; }

        #endregion


        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="TemplateQuestion" /> class.
        /// </summary>
        public TemplateQuestion()
        {
        }

        /// <summary>
        ///     Creates a new <see cref="TemplateQuestion" /> class instance from the specified <param name="question" />.
        /// </summary>
        public TemplateQuestion( PollQuestion question )
        {
            QuestionText = question.Text;
            CustomText = question.Text;
            Answers = question.Answers.Select( a => a.Text ).ToList();
        }

        #endregion
    }
}
