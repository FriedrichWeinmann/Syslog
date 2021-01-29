using System.Text.RegularExpressions;

namespace Syslog
{
    internal class RegexContainer
    {
        internal Regex Regex;
        internal string ReplaceWith;

        internal RegexContainer(string Pattern, string ReplaceWith, RegexOptions Options)
        {
            Regex = new Regex(Pattern, Options);
            this.ReplaceWith = ReplaceWith;
        }
    }
}
