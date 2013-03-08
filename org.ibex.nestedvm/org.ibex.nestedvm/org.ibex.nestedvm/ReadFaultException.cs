namespace org.ibex.nestedvm
{
    public class ReadFaultException : FaultException
    {
        public ReadFaultException(int addr)
            : base(addr)
        {
        }
    }
}