namespace org.ibex.nestedvm.util
{
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