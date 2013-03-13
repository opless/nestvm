using System;

namespace org.ibex.nestedvm
{
    public class FaultException : ExecutionException
    {
        public readonly int addr;
        public readonly Exception cause;
        public FaultException(int addr)
            : base("fault at: 0x" + (addr).ToString("X8"))
        {
            this.addr = addr;
            cause = null;
        }
        public FaultException(Exception e)
            : base(e.ToString())
        {
            addr = -1;
            cause = e;
        }
    }
}