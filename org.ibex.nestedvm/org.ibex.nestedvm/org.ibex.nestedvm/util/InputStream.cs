using System;
using System.IO;

namespace org.ibex.nestedvm.util
{
    public class InputStream : Seekable
    {
        internal sbyte[] buffer = new sbyte[4096];
        internal int bytesRead = 0;
        internal bool eof = false;
        internal int pos_Renamed;
        //internal java.io.InputStream @is;
        internal InputStream inputStream;

        public InputStream()
        {
        }
        //public InputStream(java.io.InputStream @is)
        public InputStream(InputStream inputStream)
        {
            this.inputStream = inputStream;
        }
        public InputStream(TextReader tr)
        {
            throw new NotImplementedException();
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int read(byte[] outbuf, int off, int len) throws IOException
        public override int read(sbyte[] outbuf, int off, int len)
        {
            if (pos_Renamed >= bytesRead && !eof)
            {
                readTo(pos_Renamed + 1);
            }
            len = Math.Min(len, bytesRead - pos_Renamed);
            if (len <= 0)
            {
                return -1;
            }
            Array.Copy(buffer, pos_Renamed, outbuf, off, len);
            pos_Renamed += len;
            return len;
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readTo(int target) throws IOException
        internal virtual void readTo(int target)
        {
            if (target >= buffer.Length)
            {
                sbyte[] buf2 = new sbyte[Math.Max(buffer.Length + Math.Min(buffer.Length, 65536), target)];
                Array.Copy(buffer, 0, buf2, 0, bytesRead);
                buffer = buf2;
            }
            while (bytesRead < target)
            {
                int n = inputStream.read(buffer, bytesRead, buffer.Length - bytesRead);
                if (n == -1)
                {
                    eof = true;
                    break;
                }
                bytesRead += n;
            }
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int length() throws IOException
        public override int length()
        {
            while (!eof)
            {
                readTo(bytesRead + 4096);
            }
            return bytesRead;
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int write(byte[] buf, int off, int len) throws IOException
        public override int write(sbyte[] buf, int off, int len)
        {
            throw new IOException("read-only");
        }

        public override void seek(int pos)
        {
            this.pos_Renamed = pos;
        }

        public override int pos()
        {
            return pos_Renamed;
        }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void close() throws IOException
        public override void close()
        {
            inputStream.close();
        }
    }
}