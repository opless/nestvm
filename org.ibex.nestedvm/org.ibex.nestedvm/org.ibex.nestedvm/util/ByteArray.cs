using System;
using System.IO;

namespace org.ibex.nestedvm.util
{
    public class ByteArray : Seekable
    {
        protected internal sbyte[] data;
        protected internal int pos_Renamed;
        internal readonly bool writable;

        public ByteArray(sbyte[] data, bool writable)
        {
            this.data = data;
            this.pos_Renamed = 0;
            this.writable = writable;
        }

        public override int read(sbyte[] buf, int off, int len)
        {
            len = Math.Min(len, data.Length - pos_Renamed);
            if (len <= 0)
            {
                return -1;
            }
            Array.Copy(data, pos_Renamed, buf, off, len);
            pos_Renamed += len;
            return len;
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int write(byte[] buf, int off, int len) throws IOException
        public override int write(sbyte[] buf, int off, int len)
        {
            if (!writable)
            {
                throw new IOException("read-only data");
            }
            len = Math.Min(len, data.Length - pos_Renamed);
            if (len <= 0)
            {
                throw new IOException("no space");
            }
            Array.Copy(buf, off, data, pos_Renamed, len);
            pos_Renamed += len;
            return len;
        }

        public override int length()
        {
            return data.Length;
        }

        public override int pos()
        {
            return pos_Renamed;
        }

        public override void seek(int pos)
        {
            this.pos_Renamed = pos;
        }

        public override void close() //noop
        {
        }
    }
}