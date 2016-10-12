using System.Collections.Generic;

namespace ForumParser.Models
{
    public class ChartTemplate
    {
        #region Auto-properties

        public double Width { get; set; }
        public double Height { get; set; }

        public ICollection<TemplateQuestion> Questions { get; set; }
        public ICollection<string> AnswersText { get; set; }

        #endregion
    }

    public class TemplateQuestion
    {
        #region Auto-properties

        /// <summary>
        ///     The origianl text of the question.
        /// </summary>
        public string QuestionText { get; set; }

        /// <summary>
        ///     The text overriden by user.
        /// </summary>
        public string Text { get; set; }

        public IList<string> Answers { get; set; }

        #endregion
    }
}
