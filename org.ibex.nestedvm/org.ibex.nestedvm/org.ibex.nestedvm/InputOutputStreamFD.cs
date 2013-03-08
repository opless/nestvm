using System;
using System.IO;
using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    public class InputOutputStreamFD : FD
    {
        internal readonly InputStream @is;
        internal readonly OutputStream os;

        public InputOutputStreamFD(InputStream @is)
            : this(@is, null)
        {
        }
        public InputOutputStreamFD(OutputStream os)
            : this(null, os)
        {
        }
        public InputOutputStreamFD(InputStream @is, OutputStream os)
        {
            this.@is = @is;
            this.os = os;
            if (@is == null && os == null)
            {
                throw new System.ArgumentException("at least one stream must be supplied");
            }
        }

        public override int flags()
        {
            if (@is != null && os != null)
            {
                return O_RDWR;
            }
            if (@is != null)
            {
                return O_RDONLY;
            }
            if (os != null)
            {
                return O_WRONLY;
            }
            throw new Exception("should never happen");
        }

        protected internal override void _close()
        {
            if (@is != null) //ignore
            {
                try
                {
                    @is.close();
                }
                catch (IOException e)
                {
                }
            }
            if (os != null) //ignore
            {
                try
                {
                    os.close();
                }
                catch (IOException e)
                {
                }
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int read(byte[] a, int off, int length) throws ErrnoException
        public override int read(sbyte[] a, int off, int length)
        {
            if (@is == null)
            {
                return base.read(a, off, length);
            }
            try
            {
                int n = @is.read(a, off, length);
                return n < 0 ? 0 : n;
            }
            catch (IOException e)
            {
                throw new ErrnoException(EIO);
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int write(byte[] a, int off, int length) throws ErrnoException
        public override int write(sbyte[] a, int off, int length)
        {
            if (os == null)
            {
                return base.write(a, off, length);
            }
            try
            {
                os.write(a, off, length);
                return length;
            }
            catch (IOException e)
            {
                throw new ErrnoException(EIO);
            }
        }

        protected internal override FStat _fstat()
        {
            return new SocketFStat();
        }
    }
}