using System.Collections.Generic;

namespace ForumParserWPF.Models
{
    public class Poll
    {
        #region Auto-properties

        public IList<PollQuestion> Questions { get; set; }

        #endregion
    }
}