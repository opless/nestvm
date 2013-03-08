using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    public class SecurityManager
    {
        public virtual bool allowRead(File f)
        {
            return true;
        }
        public virtual bool allowWrite(File f)
        {
            return true;
        }
        public virtual bool allowStat(File f)
        {
            return true;
        }
        public virtual bool allowUnlink(File f)
        {
            return true;
        }
    }
}