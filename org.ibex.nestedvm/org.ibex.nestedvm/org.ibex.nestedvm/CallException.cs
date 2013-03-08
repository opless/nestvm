using System;

namespace org.ibex.nestedvm
{
    public class CallException : Exception
    {
        public CallException(string s)
            : base(s)
        {
        }
    }
}