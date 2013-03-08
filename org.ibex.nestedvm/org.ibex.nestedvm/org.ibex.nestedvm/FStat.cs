namespace org.ibex.nestedvm
{
    public abstract class FStat
    {
        public const int S_IFIFO = 0x10000;
        public const int S_IFCHR = 0x20000;
        public const int S_IFDIR = 0x40000;
        public const int S_IFREG = 0x80000;
        public const int S_IFSOCK = 0xC0000;

        public virtual int mode()
        {
            return 0;
        }
        public virtual int nlink()
        {
            return 0;
        }
        public virtual int uid()
        {
            return 0;
        }
        public virtual int gid()
        {
            return 0;
        }
        public virtual int size()
        {
            return 0;
        }
        public virtual int atime()
        {
            return 0;
        }
        public virtual int mtime()
        {
            return 0;
        }
        public virtual int ctime()
        {
            return 0;
        }
        public virtual int blksize()
        {
            return 512;
        }
        public virtual int blocks()
        {
            return (size() + blksize() - 1) / blksize();
        }

        public abstract int dev();
        public abstract int type();
        public abstract int inode();
    }
}