﻿using System.Collections.Generic;

namespace ForumParser.Models
{
    public class ForumTopic
    {
        #region Auto-properties

        public string Name { get; set; }

        public List<User> Users { get; set; }

        public Poll Poll { get; set; }

        public string Url { get; set; }

        public string Dummy { get; set; }

        #endregion
    }
}
