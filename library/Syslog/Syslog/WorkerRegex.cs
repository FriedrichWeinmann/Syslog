using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Syslog
{
    public class WorkerRegex : WorkerBase
    {
        internal List<RegexContainer> Transformations = new List<RegexContainer>();

        public WorkerRegex(string Server, int Port, Server ServerObject)
            : base(Server, Port, ServerObject)
        {
            RegexOptions options = RegexOptions.IgnoreCase;

            if (ServerObject.Parameters.ContainsKey("RegexOptions"))
                options = (RegexOptions)ServerObject.Parameters["RegexOptions"];

            foreach (string key in ServerObject.Parameters.Keys)
                if (!key.Equals("RegexOptions", StringComparison.OrdinalIgnoreCase))
                    Transformations.Add(new RegexContainer(key, (string)ServerObject.Parameters[key], options));
        }

        public override string Convert(string Line)
        {
            foreach (RegexContainer container in Transformations)
                Line = container.Regex.Replace(Line, container.ReplaceWith);
            return Line;
        }
    }
}
