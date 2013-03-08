using System;
using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    internal class HostFStat : FStat
    {
        internal readonly File f;
        internal readonly File sf;
        internal readonly bool executable;
        public HostFStat(File f, File sf)
            : this(f, sf, false)
        {
        }
        public HostFStat(File f, bool executable)
            : this(f, null, executable)
        {
        }
        public HostFStat(File f, File sf, bool executable)
        {
            this.f = f;
            this.sf = sf;
            this.executable = executable;
        }
        public override int dev()
        {
            return 1;
        }
        public override int inode()
        {
            throw new NotImplementedException();
            //				return f.AbsolutePath.GetHashCode() & 0x7fff;
        }
        public override int type()
        {
            throw new NotImplementedException();
            //				return f.Directory ? S_IFDIR : S_IFREG;
        }
        public override int nlink()
        {
            return 1;
        }
        public override int mode()
        {
            throw new NotImplementedException();
            /*
                  int mode = 0;
                  bool canread = f.canRead();
                  if (canread && (executable || f.Directory))
                  {
                      mode |= 0x91;
                  }
                  if (canread)
                  {
                      mode |= 0x244;
                  }
                  if (f.canWrite())
                  {
                      mode |= 0x122;
                  }
                  return mode;
          */
        }
        public override int size()
        {
            try
            {
                return sf != null ? (int)sf.length() : (int)f.length();
            }
            catch (Exception x)
            {
                return (int)f.length();
            }
        }
        public override int mtime()
        {
            throw new NotImplementedException();
            //return (int)(f.lastModified() / 1000);
        }
    }
}