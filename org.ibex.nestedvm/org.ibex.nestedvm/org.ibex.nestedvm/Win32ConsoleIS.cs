using System;
using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    internal class Win32ConsoleIS : InputStream
    {
        internal int pushedBack = -1;
        internal readonly InputStream parent;
        public Win32ConsoleIS(InputStream parent)
        {
            this.parent = parent;
        }
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int read() throws IOException
        public virtual int read()
        {
            if (pushedBack != -1)
            {
                int ch = pushedBack;
                pushedBack = -1;
                return ch;
            }
            int c = parent.read();
            if (c == '\r' && (c = parent.read()) != '\n')
            {
                pushedBack = c;
                return '\r';
            }
            return c;
        }
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int read(byte[] buf, int pos, int len) throws IOException
        public virtual int read(sbyte[] buf, int pos, int len)
        {
            bool pb = false;
            if (pushedBack != -1 && len > 0)
            {
                buf[0] = (sbyte)pushedBack;
                pushedBack = -1;
                pos++;
                len--;
                pb = true;
            }
            int n = parent.read(buf, pos, len);
            if (n == -1)
            {
                return pb ? 1 : -1;
            }
            for (int i = 0; i < n; i++)
            {
                if (buf[pos + i] == '\r')
                {
                    if (i == n - 1)
                    {
                        int c = parent.read();
                        if (c == '\n')
                        {
                            buf[pos + i] = (sbyte)'\n';
                        }
                        else
                        {
                            pushedBack = c;
                        }
                    }
                    else if (buf[pos + i + 1] == '\n')
                    {
                        Array.Copy(buf, pos + i + 1, buf, pos + i, len - i - 1);
                        n--;
                    }
                }
            }
            return n + (pb ? 1 : 0);
        }
    }
}