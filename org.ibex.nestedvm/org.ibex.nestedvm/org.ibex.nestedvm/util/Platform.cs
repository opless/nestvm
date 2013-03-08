using System;
using System.Collections;
using System.Text;

// Copyright 2000-2005 the Contributors, as shown in the revision logs.
// Licensed under the Apache License 2.0 ("the License").
// You may not use this file except in compliance with the License.
using org.ibex.nestedvm.util;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Globalization;

namespace org.ibex.nestedvm.util
{


  /*
   GCCLASS_HINT: org.ibex.nestedvm.util.Platform.<clinit> org.ibex.nestedvm.util.Platform$Jdk11.<init>
   GCCLASS_HINT: org.ibex.nestedvm.util.Platform.<clinit> org.ibex.nestedvm.util.Platform$Jdk12.<init>
   GCCLASS_HINT: org.ibex.nestedvm.util.Platform.<clinit> org.ibex.nestedvm.util.Platform$Jdk13.<init>
   GCCLASS_HINT: org.ibex.nestedvm.util.Platform.<clinit> org.ibex.nestedvm.util.Platform$Jdk14.<init>
  */
  public abstract class Platform
  {
    internal Platform()
    {
    }

    private static readonly Platform p;

    static Platform()
    {
      throw new NotImplementedException(); // TODO
      /*
      float version;
      try
      {
        if (getProperty("java.vm.name").Equals("SableVM"))
        {
          version = 1.2f;
        }
        else
        {
          version = (float)Convert.ToSingle(getProperty("java.specification.version"));
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("WARNING: " + e + " while trying to find jvm version -  assuming 1.1");
        version = 1.1f;
      }
      string platformClass;
      if (version >= 1.4f)
      {
        platformClass = "Jdk14";
      }
      else if (version >= 1.3f)
      {
        platformClass = "Jdk13";
      }
      else if (version >= 1.2f)
      {
        platformClass = "Jdk12";
      }
      else if (version >= 1.1f)
      {
        platformClass = "Jdk11";
      }
      else
      {
        throw new Exception("JVM Specification version: " + version + " is too old. (see org.ibex.util.Platform to add support)");
      }

      try
      {
        p = (Platform) Type.GetType(typeof(Platform).Name + "$" + platformClass).newInstance();
      }
      catch (Exception e)
      {
        Console.WriteLine(e.ToString());
        Console.Write(e.StackTrace);
        throw new Exception("Error instansiating platform class");
      }
   */   
    }

    public static string getProperty(string key)
    {
      //  try
      //  {
      //    return System.getProperty(key);
      //  }
      //catch (SecurityException e)
      //{
      return null;
      //}
    } 


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: abstract boolean _atomicCreateFile(File f) throws IOException;
    internal abstract bool _atomicCreateFile(File f);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static boolean atomicCreateFile(File f) throws IOException
    public static bool atomicCreateFile(File f)
    {
      return p._atomicCreateFile(f);
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: abstract Seekable.Lock _lockFile(Seekable s, Stream raf, long pos, long size, boolean shared) throws IOException;
    internal abstract Lock _lockFile(Seekable s, Stream raf, long pos, long size, bool shared);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static Seekable.Lock lockFile(Seekable s, Stream raf, long pos, long size, boolean shared) throws IOException
    public static Lock lockFile(Seekable s, Stream raf, long pos, long size, bool shared)
    {
      return p._lockFile(s, raf, pos, size, shared);
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: abstract void _socketHalfClose(Socket s, boolean output) throws IOException;
    internal abstract void _socketHalfClose(Socket s, bool output);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void socketHalfClose(Socket s, boolean output) throws IOException
    public static void socketHalfClose(Socket s, bool output)
    {
      p._socketHalfClose(s, output);
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: abstract void _socketSetKeepAlive(Socket s, boolean on) throws SocketException;
    internal abstract void _socketSetKeepAlive(Socket s, bool on);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void socketSetKeepAlive(Socket s, boolean on) throws SocketException
    public static void socketSetKeepAlive(Socket s, bool on)
    {
      p._socketSetKeepAlive(s, on);
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: abstract IPAddress _inetAddressFromBytes(byte[] a) throws UnknownHostException;
    internal abstract IPAddress _inetAddressFromBytes(sbyte[] a);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static IPAddress inetAddressFromBytes(byte[] a) throws UnknownHostException
    public static IPAddress inetAddressFromBytes(sbyte[] a)
    {
      return p._inetAddressFromBytes(a);
    }

    internal abstract string _timeZoneGetDisplayName(TimeZone tz, bool dst, bool showlong, CultureInfo l);

    public static string timeZoneGetDisplayName(TimeZone tz, bool dst, bool showlong, CultureInfo l)
    {
      return p._timeZoneGetDisplayName(tz, dst, showlong, l);
    }

    public static string timeZoneGetDisplayName(TimeZone tz, bool dst, bool showlong)
    {
      return timeZoneGetDisplayName(tz, dst, showlong, CultureInfo.CurrentCulture);
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: abstract void _setFileLength(Stream f, int length) throws IOException;
    internal abstract void _setFileLength(Stream f, int length);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void setFileLength(Stream f, int length) throws IOException
    public static void setFileLength(Stream f, int length)
    {
      p._setFileLength(f, length);
    }

    internal abstract File[] _listRoots();

    public static File[] listRoots()
    {
      return p._listRoots();
    }

    internal abstract File _getRoot(File f);

    public static File getRoot(File f)
    {
      return p._getRoot(f);
    }

    internal class Jdk11 : Platform
    {
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: boolean _atomicCreateFile(File f) throws IOException
      internal override bool _atomicCreateFile(File f)
      {
        // This is not atomic, but its the best we can do on jdk 1.1
        throw new NotImplementedException();
        /*
        if (f.exists())
        {
          return false;
        }
        (new FileOutputStream(f)).close();
        return true;
        */    
      }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Seekable.Lock _lockFile(Seekable s, Stream raf, long p, long size, boolean shared) throws IOException
      internal override Lock _lockFile(Seekable s, Stream raf, long p, long size, bool shared)
      {
        throw new IOException("file locking requires jdk 1.4+");
      }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void _socketHalfClose(Socket s, boolean output) throws IOException
      internal override void _socketHalfClose(Socket s, bool output)
      {
        throw new IOException("half closing sockets not supported");
      }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: IPAddress _inetAddressFromBytes(byte[] a) throws UnknownHostException
      internal override IPAddress _inetAddressFromBytes(sbyte[] a)
      {
        if (a.Length != 4)
        {
          throw new /*UnknownHost*/Exception("only ipv4 addrs supported");
        }
        byte[] b = new byte[4];
        Array.ConstrainedCopy(a, 0, b, 0, 4);
        //return IPAddress.getByName("" + (a[0] & 0xff) + "." + (a[1] & 0xff) + "." + (a[2] & 0xff) + "." + (a[3] & 0xff));
        return new IPAddress(b);
      }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void _socketSetKeepAlive(Socket s, boolean on) throws SocketException
      internal override void _socketSetKeepAlive(Socket s, bool on)
      {
        if (on)
        {
          throw new /*Socket*/Exception("keepalive not supported");
        }
      }

      internal override string _timeZoneGetDisplayName(TimeZone tz, bool dst, bool showlong, CultureInfo l)
      {
        /*
        string[][] zs = (new DateFormatSymbols(l)).ZoneStrings;
        string id = tz.ID;
        for (int i = 0;i < zs.Length;i++)
        {
          if (zs[i][0].Equals(id))
          {
            return zs[i][dst ? (showlong ? 3 : 4) : (showlong ? 1 : 2)];
          }
        }
        StringBuilder sb = new StringBuilder("GMT");
        int off = tz.RawOffset / 1000;
        if (off < 0)
        {
          sb.Append("-");
          off = -off;
        }
        else
        {
          sb.Append("+");
        }
        sb.Append(off / 3600);
        off = off % 3600;
        if (off > 0)
        {
          sb.Append(":").Append(off / 60);
        }
          off = off % 60;
        if (off > 0)
        {
          sb.Append(":").Append(off);
        }
        return sb.ToString();
    */
        throw new NotImplementedException();    
      }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void _setFileLength(Stream f, int length) throws IOException
      internal override void _setFileLength(Stream f, int length)
      {
        /*
        InputStream @in = new FileInputStream(f.FD);
        OutputStream @out = new FileOutputStream(f.FD);

        sbyte[] buf = new sbyte[1024];
        for (int len; length > 0; length -= len)
        {
          len = @in.read(buf, 0, Math.Min(length, buf.Length));
          if (len == -1)
          {
            break;
          }
          @out.write(buf, 0, len);
        }
        if (length == 0)
        {
          return;
        }

        // fill the rest of the space with zeros
        for (int i = 0; i < buf.Length; i++)
        {
          buf[i] = 0;
        }
        while (length > 0)
        {
          @out.write(buf, 0, Math.Min(length, buf.Length));
          length -= buf.Length;
        } */
        throw new NotImplementedException();  
      }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Stream _truncatedStream(File f, String mode) throws IOException
      internal virtual Stream _truncatedStream(File f, string mode)
      {
        /*
        (new FileOutputStream(f)).close();
        return new Stream(f,mode);
 */
        throw new NotImplementedException();  
      }

      internal override File[] _listRoots()
      {
        /*
        string[] rootProps = new string[]{"java.home","java.class.path","java.library.path","java.io.tmpdir","java.ext.dirs","user.home","user.dir"};
        Hashtable known = new Hashtable();
        for (int i = 0;i < rootProps.Length;i++)
        {
          string prop = getProperty(rootProps[i]);
          if (prop == null)
          {
            continue;
          }
          for (;;)
          {
            string path = prop;
            int p;
            if ((p = prop.IndexOf(System.IO.Path.PathSeparator)) != -1)
            {
              path = prop.Substring(0,p);
              prop = prop.Substring(p + 1);
            }
            File root = getRoot(new File(path));
            //System.err.println(rootProps[i] + ": " + path + " -> " + root);
            known[root] = true;
            if (p == -1)
            {
              break;
            }
          }
        }
        File[] ret = new File[known.Count];
        int j = 0;
        //for (System.Collections.IEnumerator e = known.Keys.GetEnumerator();e.hasMoreElements();)
        foreach(var x in known.Keys)
        {
          ret[j++] = (File) e.nextElement();
        }
        return ret; */
        throw new NotImplementedException();  
      }

      internal override File _getRoot(File f)
      {
        /*
        if (!f.Absolute)
        {
          f = new File(f.AbsolutePath);
        }
        string p;
        while ((p = f.Parent) != null)
        {
          f = new File(p);
        }
        if (f.Path.length() == 0) // work around a classpath bug
        {
          f = new File("/");
        }
        return f;
      } */
        throw new NotImplementedException();  
      }

      internal class Jdk12 : Jdk11
      {
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: boolean _atomicCreateFile(File f) throws IOException
        internal override bool _atomicCreateFile(File f)
        {
          /*
        return f.createNewFile();
     */
          throw new NotImplementedException();      
        }

        internal override string _timeZoneGetDisplayName(TimeZone tz, bool dst, bool showlong, CultureInfo l)
        {
          return tz.StandardName;
          //return tz.getDisplayName(dst,showlong ? TimeZone.LONG : TimeZone.SHORT, l);
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void _setFileLength(Stream f, int length) throws IOException
        internal override void _setFileLength(Stream f, int length)
        {
          /*
        f.Length = length; */
          throw new NotImplementedException();  
        }

        internal override File[] _listRoots()
        {
          //TODO: this is wrong.
          /*
        throw new NotSupportedException();
        string roots = Directory.GetLogicalDrives();
        File[] ret = new File[roots.Length];
        //return File.listRoots();
        for (int i=0; i< roots.Length; i++)
        {
          ret[i] = new File(roots[i]);
        }
        return ret; */
          throw new NotImplementedException();  
        }
      }

      internal class Jdk13 : Jdk12
      {
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void _socketHalfClose(Socket s, boolean output) throws IOException
        internal override void _socketHalfClose(Socket s, bool output)
        {
          /*
        if (output)
        {
          s.shutdownOutput();
        }
        else
        {
          s.shutdownInput();
        } */
          throw new NotImplementedException();  
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void _socketSetKeepAlive(Socket s, boolean on) throws SocketException
        internal override void _socketSetKeepAlive(Socket s, bool on)
        {
          /*
        s.KeepAlive = on;
     */
          throw new NotImplementedException();      
        }
      }

      internal class Jdk14 : Jdk13
      {
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: IPAddress _inetAddressFromBytes(byte[] a) throws UnknownHostException
        internal override IPAddress _inetAddressFromBytes(sbyte[] a)
        {
          /*
        return new IPAddress(a);
         */
          throw new NotImplementedException();  
          //return IPAddress.getByAddress(a);
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Seekable.Lock _lockFile(Seekable s, Stream r, long pos, long size, boolean shared) throws IOException
        internal override Lock _lockFile(Seekable s, Stream r, long pos, long size, bool shared)
        {
          /*
        FileLock flock;
        try
        {
          flock = pos == 0 && size == 0 ? r.Channel.@lock() : r.Channel.tryLock(pos, size, shared);
        }
        catch (OverlappingFileLockException e)
        {
          flock = null;
        }
        if (flock == null) // region already locked
        {
          return null;
        }
        return new Jdk14FileLock(s, flock);
    */
          throw new NotImplementedException();
        }
      }

      private sealed class Jdk14FileLock : /*Seekable.*/ Lock
      {
        internal readonly Seekable s;
        //internal readonly FileLock l;

        //internal Jdk14FileLock(Seekable sk, FileLock flock)
        //{
        //  s = sk;
        //  l = flock;
        //}
        public override Seekable seekable()
        {
          return s;
        }

        public override bool Shared
        {
          get
          {
            throw new NotImplementedException();

            //return l.Shared;
          }
        }

        public override bool Valid
        {
          get
          {
            throw new NotImplementedException();
            //return l.Valid;
          }
        }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void release() throws IOException
        public override void release()
        {
          throw new NotImplementedException();
          //l.release();
        }

        public override long position()
        {
          throw new NotImplementedException();
          //return l.position();
        }

        public override long size()
        {
          throw new NotImplementedException();
          //return l.size();
        }

        public override string ToString()
        {
          throw new NotImplementedException();
          //return l.ToString();
        }
      }
    }

  }
}