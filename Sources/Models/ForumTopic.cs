using System.Collections.Generic;

namespace ForumParserWPF.Models
{
    public class ParserRules
    {
        #region Fields

        public string UserIdRegex;

        #endregion
    }

    public class ForumTopic
    {
        #region Auto-properties

        public string Name { get; set; }

        public List<User> Users { get; set; }

        public Poll Poll { get; set; }

        #endregion
    }
}
