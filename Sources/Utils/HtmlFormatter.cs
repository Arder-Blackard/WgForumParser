using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Extensions;
using ForumParser.Models;

namespace ForumParser.Utils
{
    internal static class HtmlFormatter
    {
        #region Constants

        private const string PostBlockFormat =
            "<div class=\"post_block{0}\"><div class=\"post_body\"><div itemprop=\"commentText\" class=\"post\">{1}</div></div></div>";

        private const string MessagesCss = @"<style type='text/css'>
body, div, dl, dt, dd, ul, ol, li, h1, h2, h3, h4, h5, h6, pre, form, fieldset, input, textarea, p, blockquote, th, td {
    margin: 0;
    padding: 0;
}
body {
    font: normal 13px helvetica, arial, sans-serif;
    position: relative;
}
html, body {
    background: #dcd9cd;
    color: #5a5a5a;
}
.post_block {
    background: #f3f3f2;
    padding: 8px;
    border-bottom: 2px solid #DCD9CD;
}
.post_block__colored {
    background: #eeedea;
}
.post_body .post {
    line-height: 1.6;
    font-size: 14px;
    word-wrap: break-word;
}
.post_body .post {
    padding: 15px 0 0;
    color: #282828;
}
p {
    display: block;
}
h3, strong {
    font-weight: bold;
}
</style>";

        #endregion


        #region Public methods

        public static string FormatUserMessages( IList<Message> messages )
        {
            Func<Message, int, string> messageFormatter =
                ( m, i ) => string.Format( PostBlockFormat, i%2 == 0 ? " post_block__colored" : "", m.Html );

            return $"{MessagesCss}<html><body>{messages.Select( messageFormatter ).JoinToString()}<body></html>";
        }

        #endregion
    }
}
