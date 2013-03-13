namespace org.ibex.nestedvm
{
    public class CpuState
    {
        public CpuState() // noop
        {
        }
        /* GPRs */
        public int[] r = new int[32];
        /* Floating point regs */
        public int[] f = new int[32];
        public int hi, lo;
        public int fcsr;
        public int pc;

        public virtual CpuState dup()
        {
            CpuState c = new CpuState();
            c.hi = hi;
            c.lo = lo;
            c.fcsr = fcsr;
            c.pc = pc;
            for (int i = 0; i < 32; i++)
            {
                c.r[i] = r[i];
                c.f[i] = f[i];
            }
            return c;
        }
    }
}