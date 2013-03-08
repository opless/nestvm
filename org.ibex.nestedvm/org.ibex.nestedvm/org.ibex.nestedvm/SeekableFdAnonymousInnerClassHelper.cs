using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    internal class SeekableFdAnonymousInnerClassHelper : SeekableFd
    {
        private readonly Runtime outerInstance;

        private File f;
        private new object data;
        private File sf;

        public SeekableFdAnonymousInnerClassHelper(Runtime outerInstance, File sf, int flags, File f, object data)
            : base(sf, flags)
        {
            this.outerInstance = outerInstance;
            this.f = f;
            this.data = data;
            this.sf = sf;
        }

        protected internal override FStat _fstat()
        {
            return outerInstance.hostFStat(f, sf, data);
        }
    }
}