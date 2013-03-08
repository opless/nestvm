using System;

namespace org.ibex.nestedvm
{
    public class ExecutionException : Exception
    {
        internal string message = "(null)";
        internal string location = "(unknown)";
        public ExecutionException() // noop
        {
        }
        public ExecutionException(string s)
        {
            if (s != null)
            {
                message = s;
            }
        }
        internal virtual string Location
        {
            set
            {
                location = value == null ? "(unknown)" : value;
            }
        }
        public string Message
        {
            get
            {
                return message + " at " + location;
            }
        }
    }
}