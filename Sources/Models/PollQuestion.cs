using System.Collections.Generic;

namespace ForumParserWPF.Models
{
    public class PollQuestion
    {
        #region Auto-properties

        public string Text { get; set; }
        public IList<PollAnswerBase> Answers { get; set; }

        #endregion


        #region Public methods

        public override string ToString() => Text;

        #endregion
    }
}