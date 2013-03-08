// Copyright 2000-2005 the Contributors, as shown in the revision logs.
// Licensed under the Apache License 2.0 ("the License").
// You may not use this file except in compliance with the License.
using System.IO;

namespace org.ibex.nestedvm.util
{
  public abstract class Seekable
  {
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract int read(byte[] buf, int offset, int length) throws IOException;
    public abstract int read(sbyte[] buf, int offset, int length);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract int write(byte[] buf, int offset, int length) throws IOException;
    public abstract int write(sbyte[] buf, int offset, int length);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract int length() throws IOException;
    public abstract int length();
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract void seek(int pos) throws IOException;
    public abstract void seek(int pos);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract void close() throws IOException;
    public abstract void close();
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract int pos() throws IOException;
    public abstract int pos();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void sync() throws IOException
    public virtual void sync()
    {
      throw new IOException("sync not implemented for " + this.GetType());
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void resize(long length) throws IOException
    public virtual void resize(long length)
    {
      throw new IOException("resize not implemented for " + this.GetType());
    }
    /// <summary>
    /// If pos == 0 and size == 0 lock covers whole file. </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Lock lock(long pos, long size, boolean shared) throws IOException
    public virtual Lock @lock(long pos, long size, bool shared)
    {
      throw new IOException("lock not implemented for " + this.GetType());
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int read() throws IOException
    public virtual int read()
    {
      sbyte[] buf = new sbyte[1];
      int n = read(buf, 0, 1);
      return n == -1 ? - 1 : buf [0] & 0xff;
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int tryReadFully(byte[] buf, int off, int len) throws IOException
    public virtual int tryReadFully(sbyte[] buf, int off, int len)
    {
      int total = 0;
      while (len > 0)
      {
        int n = read(buf, off, len);
        if (n == -1)
        {
          break;
        }
        off += n;
        len -= n;
        total += n;
      }
      return total == 0 ? - 1 : total;
    }
  }
}