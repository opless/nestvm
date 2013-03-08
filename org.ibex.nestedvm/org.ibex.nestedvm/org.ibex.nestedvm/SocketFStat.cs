namespace org.ibex.nestedvm
{
    public class SocketFStat : FStat
    {
        public override int dev()
        {
            return -1;
        }
        public override int type()
        {
            return S_IFSOCK;
        }
        public override int inode()
        {
            return GetHashCode() & 0x7fff;
        }
    }
}