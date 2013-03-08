using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    internal class TerminalFD : InputOutputStreamFD
    {
        public TerminalFD(InputStream @is)
            : this(@is, null)
        {
        }
        public TerminalFD(OutputStream os)
            : this(null, os)
        {
        }
        public TerminalFD(InputStream @is, OutputStream os)
            : base(@is, os)
        {
        }
        protected internal override void _close() // noop
        {
        }
        protected internal override FStat _fstat()
        {
            return new SocketFStatAnonymousInnerClassHelper(this);
        }

        internal class SocketFStatAnonymousInnerClassHelper : SocketFStat
        {
            private readonly TerminalFD outerInstance;

            public SocketFStatAnonymousInnerClassHelper(TerminalFD outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override int type()
            {
                return S_IFCHR;
            }
            public override int mode()
            {
                return 0x300;
            }
        }
    }
}