using System;
using System.IO;

namespace org.ibex.nestedvm.util
{
    public class OutputStream : Seekable
    {
        public OutputStream()
        {
            throw new NotImplementedException();
        }
        public OutputStream(TextWriter tw)
        {
            throw new NotImplementedException();
        }
        #region implemented abstract members of Seekable
        public override int read(sbyte[] buf, int offset, int length)
        {
            throw new NotImplementedException();
        }
        public override int write(sbyte[] buf, int offset, int length)
        {
            throw new NotImplementedException();
        }
        public override int length()
        {
            throw new NotImplementedException();
        }
        public override void seek(int pos)
        {
            throw new NotImplementedException();
        }
        public override void close()
        {
            throw new NotImplementedException();
        }
        public override int pos()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}