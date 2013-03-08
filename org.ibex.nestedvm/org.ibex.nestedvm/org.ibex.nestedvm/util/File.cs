using System;
using System.IO;

namespace org.ibex.nestedvm.util
{
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
}