namespace org.ibex.nestedvm
{
    public class WriteFaultException : FaultException
    {
        public WriteFaultException(int addr)
            : base(addr)
        {
        }
    }
}