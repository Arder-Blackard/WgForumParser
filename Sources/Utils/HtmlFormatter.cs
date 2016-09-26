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

        private const string MessageJs = @"<script type=""text/javascript"">
function toggle(el) {
    if (el.style.display == 'none') {
        el.style.display = '';
        return true;
    } else {
        el.style.display = 'none';
        return false;
    }
}
function wireUpSpoiler(button, content) {
    button.onclick = function() {
        if ( toggle(content) )
            button.value = 'Hide';
        else
            button.value = 'Show';
    }
}
function ready() {
    var spoilers = document.querySelectorAll( '.bbc_spoiler' );
    for (var i = 0; i < spoilers.length; i++) {
        var spoiler = spoilers[i];
        var button = spoiler.querySelector('.bbc_spoiler_show');
        var content = spoiler.querySelector('.bbc_spoiler_content');
        wireUpSpoiler( button, content );
    }
} 
document.addEventListener(""DOMContentLoaded"", ready);
</script>";

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
input.bbc_spoiler_show {
    font-size: 10px;
    margin: 0px;
    padding: 0px;
    font-weight: normal;
    color: #000;
    font-style: normal;
    text-decoration: none !important;
}
div.bbc_spoiler_wrapper {
    border: 1px solid #777;
    padding: 4px;
}
input, select {
    font: normal 13px helvetica, arial, sans-serif;
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
div.post ol, div.post ul, div.post dl {
    padding: 0 40px;
}
div.post ol {
    list-style-type: decimal;
}
ol, ul {
    list-style: none;
}
</style>";

        #endregion


        #region Public methods

        public static string FormatUserMessages( IList<Message> messages )
        {
            Func<Message, int, string> messageFormatter =
                ( m, i ) => string.Format( PostBlockFormat, i%2 == 0 ? " post_block__colored" : "", m.Html );

            return $"<html><meta charset=\"UTF-8\">{MessagesCss}{MessageJs}<body>{messages.Select( messageFormatter ).JoinToString()}<body></html>";
        }

        #endregion
    }
}
