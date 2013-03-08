using System;

namespace org.ibex.nestedvm
{
    internal class ErrnoException : Exception
    {
        public int errno;
        public ErrnoException(int errno)
            : base("Errno: " + errno)
        {
            this.errno = errno;
        }
    }
}