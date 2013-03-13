using System.IO;
using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    /// <summary>
    /// FileDescriptor class for normal files </summary>
    public abstract class SeekableFd : FD
    {
        internal readonly int flags_Renamed;
        internal readonly Seekable data;

        internal SeekableFd(Seekable data, int flags)
        {
            this.data = data;
            this.flags_Renamed = flags;
        }

        protected internal override abstract FStat _fstat();
        public override int flags()
        {
            return flags_Renamed;
        }

        internal override Seekable seekable()
        {
            return data;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int seek(int n, int whence) throws ErrnoException
        public override int seek(int n, int whence)
        {
            try
            {
                switch (whence)
                {
          case Runtime.SEEK_SET:
                        break;
          case Runtime.SEEK_CUR:
                        n += data.pos();
                        break;
          case Runtime.SEEK_END:
                        n += data.length();
                        break;
                    default:
                        return -1;
                }
                data.seek(n);
                return n;
            }
            catch (IOException e)
            {
        throw new ErrnoException(Runtime.ESPIPE);
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int write(byte[] a, int off, int length) throws ErrnoException
        public override int write(sbyte[] a, int off, int length)
        {
      if ((flags_Renamed & 3) == Runtime.RD_ONLY)
            {
        throw new ErrnoException(Runtime.EBADFD);
            }
            // NOTE: There is race condition here but we can't fix it in pure java
      if ((flags_Renamed & Runtime.O_APPEND) != 0)
            {
        seek(0, Runtime.SEEK_END);
            }
            try
            {
                return data.write(a, off, length);
            }
            catch (IOException e)
            {
        throw new ErrnoException(Runtime.EIO);
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int read(byte[] a, int off, int length) throws ErrnoException
        public override int read(sbyte[] a, int off, int length)
        {
      if ((flags_Renamed & 3) == Runtime.WR_ONLY)
            {
        throw new ErrnoException(Runtime.EBADFD);
            }
            try
            {
                int n = data.read(a, off, length);
                return n < 0 ? 0 : n;
            }
            catch (IOException e)
            {
        throw new ErrnoException(Runtime.EIO);
            }
        }

        protected internal override void _close() //ignore
        {
            try
            {
                data.close();
            }
            catch (IOException e)
            {
            }
        }
    }
}