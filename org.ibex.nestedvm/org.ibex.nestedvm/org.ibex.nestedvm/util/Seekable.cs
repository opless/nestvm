using System;

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

  public class File : Seekable
  {
    internal readonly File file;
    internal readonly Stream raf;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public File(String fileName) throws IOException
    public File(string fileName) : this(fileName,false)
    {
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public File(String fileName, boolean writable) throws IOException
    public File(string fileName, bool writable) : this(new File(fileName),writable,false)
    {
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public File(java.io.File file, boolean writable, boolean truncate) throws IOException
    public File(File file, bool writable, bool truncate)
    {
      throw new NotImplementedException();
      /*
      this.file = file;
      string mode = writable ? "rw" : "r";
      raf = new Stream(file, mode);
      if (truncate)
      {
        Platform.setFileLength(raf, 0);
      }
      */
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int read(byte[] buf, int offset, int length) throws IOException
    public override int read(sbyte[] buf, int offset, int length)
    {
      throw new NotImplementedException();
      //return raf.Read(buf, offset, length);
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int write(byte[] buf, int offset, int length) throws IOException
    public override int write(sbyte[] buf, int offset, int length)
    {
      throw new NotImplementedException();
      //raf.Write(buf, offset, length);
      return length;
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void sync() throws IOException
    public override void sync()
    {
      throw new NotImplementedException();
      //raf.FD.sync();
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void seek(int pos) throws IOException
    public override void seek(int pos)
    {
      throw new NotImplementedException();
      //raf.Seek(pos);
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int pos() throws IOException
    public override int pos()
    {
      return (int)raf.Position;
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int length() throws IOException
    public override int length()
    {
      return (int)raf.Length;
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void close() throws IOException
    public override void close()
    {
      raf.Close();
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void resize(long length) throws IOException
    public override void resize(long length)
    {
      Platform.setFileLength(raf, (int)length);
    }

    public override bool Equals(object o)
    {
      return o != null && o is File && file.Equals(((File)o).file);
    }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Lock lock(long pos, long size, boolean shared) throws IOException
    public override Lock @lock(long pos, long size, bool shared)
    {
      return Platform.lockFile(this, raf, pos, size, shared);
    }
  }
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

  public abstract class Lock
  {
    internal object owner = null;

    public abstract Seekable seekable();

    public abstract bool Shared { get; }

    public abstract bool Valid { get; }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract void release() throws IOException;
    public abstract void release();

    public abstract long position();

    public abstract long size();

    public virtual object Owner
    {
      set
      {
        owner = value;
      }
      get
      {
        return owner;
      }
    }

    public bool contains(int start, int len)
    {
      return start >= position() && position() + size() >= start + len;
    }

    public bool contained(int start, int len)
    {
      return start < position() && position() + size() < start + len;
    }

    public bool overlaps(int start, int len)
    {
      return contains(start, len) || contained(start, len);
    }
  }
  

}