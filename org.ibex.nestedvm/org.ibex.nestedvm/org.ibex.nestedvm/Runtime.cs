using System;
using System.Text;
using System.Threading;

// Copyright 2000-2005 the Contributors, as shown in the revision logs.
// Licensed under the Apache License 2.0 ("the License").
// You may not use this file except in compliance with the License.

// Copyright 2003 Brian Alliet
// Based on org.xwt.imp.MIPS by Adam Megacz
// Portions Copyright 2003 Adam Megacz
using System.IO;

namespace org.ibex.nestedvm
{

	using org.ibex.nestedvm.util;

  public abstract partial class Runtime //: //ICloneable //UsermodeConstants, Registers, ICloneable
	{
		public const string VERSION = "1.0";

		/// <summary>
		/// True to write useful diagnostic information to stderr when things go wrong </summary>
		internal const bool STDERR_DIAG = true;

		/// <summary>
		/// Number of bits to shift to get the page number (1<<<pageShift == pageSize) </summary>
		protected internal readonly int pageShift;
		/// <summary>
		/// Bottom of region of memory allocated to the stack </summary>
		private readonly int stackBottom;

		/// <summary>
		/// Readable main memory pages </summary>
		protected internal int[][] readPages;
		/// <summary>
		/// Writable main memory pages.
		///    If the page is writable writePages[x] == readPages[x]; if not writePages[x] == null. 
		/// </summary>
		protected internal int[][] writePages;

		/// <summary>
		/// The address of the end of the heap </summary>
		private int heapEnd;

		/// <summary>
		/// Number of guard pages to keep between the stack and the heap </summary>
		private const int STACK_GUARD_PAGES = 4;

		/// <summary>
		/// The last address the executable uses (other than the heap/stack) </summary>
		protected internal abstract int heapStart();

		/// <summary>
		/// The program's entry point </summary>
		protected internal abstract int entryPoint();

		/// <summary>
		/// The location of the _user_info block (or 0 is there is none) </summary>
		protected internal virtual int userInfoBase()
		{
			return 0;
		}
		protected internal virtual int userInfoSize()
		{
			return 0;
		}

		/// <summary>
		/// The location of the global pointer </summary>
		protected internal abstract int gp();

		/// <summary>
		/// When the process started </summary>
		private long startTime;

		/// <summary>
		/// Program is executing instructions </summary>
		public const int RUNNING = 0; // Horrible things will happen if this isn't 0
		/// <summary>
		///  Text/Data loaded in memory </summary>
		public const int STOPPED = 1;
		/// <summary>
		/// Prgram has been started but is paused </summary>
		public const int PAUSED = 2;
		/// <summary>
		/// Program is executing a callJava() method </summary>
		public const int CALLJAVA = 3;
		/// <summary>
		/// Program has exited (it cannot currently be restarted) </summary>
		public const int EXITED = 4;
		/// <summary>
		/// Program has executed a successful exec(), a new Runtime needs to be run (used by UnixRuntime) </summary>
		public const int EXECED = 5;

		/// <summary>
		/// The current state </summary>
		protected internal int state = STOPPED;
		/// <seealso cref= Runtime#state state </seealso>
		public int State
		{
			get
			{
				return state;
			}
		}

		/// <summary>
		/// The exit status if the process (only valid if state==DONE) </summary>
		///    <seealso cref= Runtime#state  </seealso>
		private int exitStatus_Renamed;
		public ExecutionException exitException;

		/// <summary>
		/// Table containing all open file descriptors. (Entries are null if the fd is not in use </summary>
		internal FD[] fds; // package-private for UnixRuntime
		internal bool[] closeOnExec;

		/// <summary>
		/// Pointer to a SecurityManager for this process </summary>
		internal SecurityManager sm;
		public virtual SecurityManager SecurityManager
		{
			set
			{
				this.sm = value;
			}
		}

		/// <summary>
		/// Pointer to a callback for the call_java syscall </summary>
		private ICallJavaCB callJavaCB;
		public virtual ICallJavaCB CallJavaCB
		{
			set
			{
				this.callJavaCB = value;
			}
		}

		/// <summary>
		/// Temporary buffer for read/write operations </summary>
		private sbyte[] _byteBuf;
		/// <summary>
		/// Max size of temporary buffer </summary>
		///    <seealso cref= Runtime#_byteBuf  </seealso>
		internal const int MAX_CHUNK = 16 * 1024 * 1024 - 1024;

		/// <summary>
		/// Subclasses should actually execute program in this method. They should continue 
		///    executing until state != RUNNING. Only syscall() can modify state. It is safe 
		///    to only check the state attribute after a call to syscall() 
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected abstract void _execute() throws ExecutionException;
		protected internal abstract void _execute();

		/// <summary>
		/// Subclasses should return the address of the symbol <i>symbol</i> or -1 it it doesn't exits in this method 
		///    This method is only required if the call() function is used 
		/// </summary>
		public virtual int lookupSymbol(string symbol)
		{
			return -1;
		}

		/// <summary>
		/// Subclasses should populate a CPUState object representing the cpu state </summary>
		protected internal abstract void getCPUState(CpuState state);

		/// <summary>
		/// Subclasses should set the CPUState to the state held in <i>state</i> </summary>
		protected internal abstract CpuState CPUState {set;}

		/// <summary>
		/// True to enabled a few hacks to better support the win32 console </summary>
		internal static readonly bool win32Hacks;

		static Runtime()
		{
			string os = Platform.getProperty("os.name");
			string prop = Platform.getProperty("nestedvm.win32hacks");
			if (prop != null)
			{
				win32Hacks = (bool)Convert.ToBoolean(prop);
			}
			else
			{
				win32Hacks = os != null && os.ToLower().IndexOf("windows") != -1;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected Object clone() throws CloneNotSupportedException
		protected internal virtual object clone()
		{
      throw new NotImplementedException();
			//Runtime r = (Runtime) base.clone();
      Runtime r = null;
			r._byteBuf = null;
			r.startTime = 0;
			r.fds = new FD[OPEN_MAX];
			for (int i = 0;i < OPEN_MAX;i++)
			{
				if (fds[i] != null)
				{
					r.fds[i] = fds[i].dup();
				}
			}
			int totalPages = writePages.Length;
			r.readPages = new int[totalPages][];
			r.writePages = new int[totalPages][];
			for (int i = 0;i < totalPages;i++)
			{
				if (readPages[i] == null)
				{
					continue;
				}
				if (writePages[i] == null)
				{
					r.readPages[i] = readPages[i];
				}
				else
				{
          throw new NotImplementedException();
					//r.readPages[i] = r.writePages[i] = (int[])writePages[i].clone();
				}
			}
			return r;
		}

		protected internal Runtime(int pageSize, int totalPages) : this(pageSize, totalPages,false)
		{
		}
		protected internal Runtime(int pageSize, int totalPages, bool exec)
		{
			if (pageSize <= 0)
			{
				throw new System.ArgumentException("pageSize <= 0");
			}
			if (totalPages <= 0)
			{
				throw new System.ArgumentException("totalPages <= 0");
			}
			if ((pageSize & (pageSize-1)) != 0)
			{
				throw new System.ArgumentException("pageSize not a power of two");
			}

			int _pageShift = 0;
			while ((int)((uint)pageSize >> _pageShift) != 1)
			{
				_pageShift++;
			}
			pageShift = _pageShift;

			int heapStart = this.heapStart();
			int totalMemory = totalPages * pageSize;
			int stackSize = max(totalMemory / 512,ARG_MAX + 65536);
			int stackPages = 0;
			if (totalPages > 1)
			{
				stackSize = max(stackSize,pageSize);
				stackSize = (stackSize + pageSize - 1) & ~(pageSize-1);
				stackPages = (int)((uint)stackSize >> pageShift);
				heapStart = (heapStart + pageSize - 1) & ~(pageSize-1);
				if (stackPages + STACK_GUARD_PAGES + ((int)((uint)heapStart >> pageShift)) >= totalPages)
				{
					throw new System.ArgumentException("total pages too small");
				}
			}
			else
			{
				if (pageSize < heapStart + stackSize)
				{
					throw new System.ArgumentException("total memory too small");
				}
				heapStart = (heapStart + 4095) & ~4096;
			}

			stackBottom = totalMemory - stackSize;
			heapEnd = heapStart;

			readPages = new int[totalPages][];
			writePages = new int[totalPages][];

			if (totalPages == 1)
			{
				readPages[0] = writePages[0] = new int[pageSize >> 2];
			}
			else
			{
				for (int i = ((int)((uint)stackBottom >> pageShift));i < writePages.Length;i++)
				{
					readPages[i] = writePages[i] = new int[pageSize >> 2];
				}
			}

			if (!exec)
			{
				fds = new FD[OPEN_MAX];
				closeOnExec = new bool[OPEN_MAX];

        //InputStream stdin = win32Hacks ? new Win32ConsoleIS(System.in) : System.in;
        InputStream stdin = new InputStream(Console.In);
        addFD(new TerminalFD(stdin));
				addFD(new TerminalFD(new OutputStream(Console.Out)));
				addFD(new TerminalFD(new OutputStream(Console.Error)));
			}
		}

		/// <summary>
		/// Copy everything from <i>src</i> to <i>addr</i> initializing uninitialized pages if required. 
		///   Newly initalized pages will be marked read-only if <i>ro</i> is set 
		/// </summary>
		protected internal void initPages(int[] src, int addr, bool ro)
		{
			int pageWords = (int)((uint)(1 << pageShift)>>2);
			int pageMask = (1 << pageShift) - 1;

			for (int i = 0;i < src.Length;)
			{
				int page = (int)((uint)addr >> pageShift);
				int start = (addr & pageMask) >> 2;
				int elements = min(pageWords - start,src.Length - i);
				if (readPages[page] == null)
				{
					initPage(page,ro);
				}
				else if (!ro)
				{
					if (writePages[page] == null)
					{
						writePages[page] = readPages[page];
					}
				}
				Array.Copy(src,i,readPages[page],start,elements);
				i += elements;
				addr += elements * 4;
			}
		}

		/// <summary>
		/// Initialize <i>words</i> of pages starting at <i>addr</i> to 0 </summary>
		protected internal void clearPages(int addr, int words)
		{
			int pageWords = (int)((uint)(1 << pageShift)>>2);
			int pageMask = (1 << pageShift) - 1;

			for (int i = 0;i < words;)
			{
				int page = (int)((uint)addr >> pageShift);
				int start = (addr & pageMask) >> 2;
				int elements = min(pageWords - start,words - i);
				if (readPages[page] == null)
				{
					readPages[page] = writePages[page] = new int[pageWords];
				}
				else
				{
					if (writePages[page] == null)
					{
						writePages[page] = readPages[page];
					}
					for (int j = start;j < start + elements;j++)
					{
						writePages[page][j] = 0;
					}
				}
				i += elements;
				addr += elements * 4;
			}
		}

		/// <summary>
		/// Copies <i>length</i> bytes from the processes memory space starting at
		///    <i>addr</i> INTO a java byte array <i>a</i> 
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void copyin(int addr, byte[] buf, int count) throws ReadFaultException
    public void copyin(int addr, byte[] buf, int count)
    {
      throw new NotImplementedException(); 
    }
    public void copyin(int addr, sbyte[] buf, int count)
		{
			int pageWords = (int)((uint)(1 << pageShift)>>2);
			int pageMask = pageWords - 1;

			int x = 0;
			if (count == 0)
			{
				return;
			}
			if ((addr & 3) != 0)
			{
				int word = memRead(addr & ~3);
        throw new NotImplementedException();
          /*
				switch (addr & 3)
				{
					case 1:
						buf[x++] = unchecked((sbyte)(((int)((uint)word >> 16)) & 0xff));
						if (--count == 0)
						{
							break;
						}
					case 2:
						buf[x++] = unchecked((sbyte)(((int)((uint)word >> 8)) & 0xff));
						if (--count == 0)
						{
							break;
						}
					case 3:
						buf[x++] = unchecked((sbyte)(((int)((uint)word >> 0)) & 0xff));
						if (--count == 0)
						{
							break;
						}
				}
        */
				addr = (addr & ~3) + 4;
			}
			if ((count & ~3) != 0)
			{
				int c = (int)((uint)count >> 2);
				int a = (int)((uint)addr >> 2);
				while (c != 0)
				{
					int[] page = readPages[(int)((uint)a >> (pageShift - 2))];
					if (page == null)
					{
						throw new ReadFaultException(a << 2);
					}
					int index = a & pageMask;
					int n = min(c,pageWords - index);
					for (int i = 0;i < n;i++,x += 4)
					{
						int word = page[index + i];
						buf[x + 0] = unchecked((sbyte)(((int)((uint)word >> 24)) & 0xff));
						buf[x + 1] = unchecked((sbyte)(((int)((uint)word >> 16)) & 0xff));
						buf[x + 2] = unchecked((sbyte)(((int)((uint)word >> 8)) & 0xff));
						buf[x + 3] = unchecked((sbyte)(((int)((uint)word >> 0)) & 0xff));
					}
					a += n;
					c -= n;
				}
				addr = a << 2;
				count &= 3;
			}
			if (count != 0)
			{
				int word = memRead(addr);
				switch (count)
				{
					case 3:
						buf[x + 2] = unchecked((sbyte)(((int)((uint)word >> 8)) & 0xff));
						goto case 2;
					case 2:
						buf[x + 1] = unchecked((sbyte)(((int)((uint)word >> 16)) & 0xff));
						goto case 1;
					case 1:
						buf[x + 0] = unchecked((sbyte)(((int)((uint)word >> 24)) & 0xff));
					break;
				}
			}
		}

		/// <summary>
		/// Copies <i>length</i> bytes OUT OF the java array <i>a</i> into the processes memory
		///    space at <i>addr</i> 
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void copyout(byte[] buf, int addr, int count) throws FaultException
		public void copyout(sbyte[] buf, int addr, int count)
		{
			int pageWords = (int)((uint)(1 << pageShift)>>2);
			int pageWordMask = pageWords - 1;

			int x = 0;
			if (count == 0)
			{
				return;
			}
			if ((addr & 3) != 0)
			{
				int word = memRead(addr & ~3);
        throw new NotImplementedException();
        /*
				switch (addr & 3)
				{
					case 1:
						word = (word & unchecked((int)0xff00ffff)) | ((buf[x++] & 0xff) << 16);
						if (--count == 0)
						{
							break;
						}
					case 2:
						word = (word & unchecked((int)0xffff00ff)) | ((buf[x++] & 0xff) << 8);
						if (--count == 0)
						{
							break;
						}
					case 3:
						word = (word & unchecked((int)0xffffff00)) | ((buf[x++] & 0xff) << 0);
						if (--count == 0)
						{
							break;
						}
				}
    */    
				memWrite(addr & ~3,word);
				addr += x;
			}

			if ((count & ~3) != 0)
			{
				int c = (int)((uint)count >> 2);
				int a = (int)((uint)addr >> 2);
				while (c != 0)
				{
					int[] page = writePages[(int)((uint)a >> (pageShift - 2))];
					if (page == null)
					{
						throw new WriteFaultException(a << 2);
					}
					int index = a & pageWordMask;
					int n = min(c,pageWords - index);
					for (int i = 0;i < n;i++,x += 4)
					{
						page[index + i] = ((buf[x + 0] & 0xff) << 24) | ((buf[x + 1] & 0xff) << 16) | ((buf[x + 2] & 0xff) << 8) | ((buf[x + 3] & 0xff) << 0);
					}
					a += n;
					c -= n;
				}
				addr = a << 2;
				count &= 3;
			}

			if (count != 0)
			{
				int word = memRead(addr);
				switch (count)
				{
					case 1:
						word = (word & 0x00ffffff) | ((buf[x + 0] & 0xff) << 24);
						break;
					case 2:
						word = (word & 0x0000ffff) | ((buf[x + 0] & 0xff) << 24) | ((buf[x + 1] & 0xff) << 16);
						break;
					case 3:
						word = (word & 0x000000ff) | ((buf[x + 0] & 0xff) << 24) | ((buf[x + 1] & 0xff) << 16) | ((buf[x + 2] & 0xff) << 8);
						break;
				}
				memWrite(addr,word);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void memcpy(int dst, int src, int count) throws FaultException
		public void memcpy(int dst, int src, int count)
		{
			int pageWords = (int)((uint)(1 << pageShift)>>2);
			int pageWordMask = pageWords - 1;
			if ((dst & 3) == 0 && (src & 3) == 0)
			{
				if ((count & ~3) != 0)
				{
					int c = count >> 2;
					int s = (int)((uint)src >> 2);
					int d = (int)((uint)dst >> 2);
					while (c != 0)
					{
						int[] srcPage = readPages[(int)((uint)s >> (pageShift - 2))];
						if (srcPage == null)
						{
							throw new ReadFaultException(s << 2);
						}
						int[] dstPage = writePages[(int)((uint)d >> (pageShift - 2))];
						if (dstPage == null)
						{
							throw new WriteFaultException(d << 2);
						}
						int srcIndex = s & pageWordMask;
						int dstIndex = d & pageWordMask;
						int n = min(c,pageWords - max(srcIndex,dstIndex));
						Array.Copy(srcPage,srcIndex,dstPage,dstIndex,n);
						s += n;
						d += n;
						c -= n;
					}
					src = s << 2;
					dst = d << 2;
					count &= 3;
				}
				if (count != 0)
				{
					int word1 = memRead(src);
					int word2 = memRead(dst);
					switch (count)
					{
						case 1:
							memWrite(dst, (word1 & unchecked((int)0xff000000)) | (word2 & 0x00ffffff));
							break;
						case 2:
							memWrite(dst, (word1 & unchecked((int)0xffff0000)) | (word2 & 0x0000ffff));
							break;
						case 3:
							memWrite(dst, (word1 & unchecked((int)0xffffff00)) | (word2 & 0x000000ff));
							break;
					}
				}
			}
			else
			{
				while (count > 0)
				{
					int n = min(count,MAX_CHUNK);
					sbyte[] buf = byteBuf(n);
					copyin(src,buf,n);
					copyout(buf,dst,n);
					count -= n;
					src += n;
					dst += n;
				}
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void memset(int addr, int ch, int count) throws FaultException
		public void memset(int addr, int ch, int count)
		{
			int pageWords = (int)((uint)(1 << pageShift)>>2);
			int pageWordMask = pageWords - 1;

			int fourBytes = ((ch & 0xff) << 24) | ((ch & 0xff) << 16) | ((ch & 0xff) << 8) | ((ch & 0xff) << 0);
			if ((addr & 3) != 0)
			{
				int word = memRead(addr & ~3);
        throw new NotImplementedException();
        /*
				switch (addr & 3)
				{
					case 1:
						word = (word & unchecked((int)0xff00ffff)) | ((ch & 0xff) << 16);
						if (--count == 0)
						{
							break;
						}
					case 2:
						word = (word & unchecked((int)0xffff00ff)) | ((ch & 0xff) << 8);
						if (--count == 0)
						{
							break;
						}
					case 3:
						word = (word & unchecked((int)0xffffff00)) | ((ch & 0xff) << 0);
						if (--count == 0)
						{
							break;
						}
				}
    */    
				memWrite(addr & ~3,word);
				addr = (addr & ~3) + 4;
			}
			if ((count & ~3) != 0)
			{
				int c = count >> 2;
				int a = (int)((uint)addr >> 2);
				while (c != 0)
				{
					int[] page = readPages[(int)((uint)a >> (pageShift - 2))];
					if (page == null)
					{
						throw new WriteFaultException(a << 2);
					}
					int index = a & pageWordMask;
					int n = min(c,pageWords - index);
					/* Arrays.fill(page,index,index+n,fourBytes);*/
					for (int i = index;i < index + n;i++)
					{
						page[i] = fourBytes;
					}
					a += n;
					c -= n;
				}
				addr = a << 2;
				count &= 3;
			}
			if (count != 0)
			{
				int word = memRead(addr);
				switch (count)
				{
					case 1:
						word = (int)((word & 0x00ffffff) | (fourBytes & 0xff000000));
						break;
					case 2:
            word = (int)((word & 0x0000ffff) | (fourBytes & 0xffff0000));
						break;
					case 3:
            word = (int)((word & 0x000000ff) | (fourBytes & 0xffffff00));
						break;
				}
				memWrite(addr,word);
			}
		}

		/// <summary>
		/// Read a word from the processes memory at <i>addr</i> </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int memRead(int addr) throws ReadFaultException
		public int memRead(int addr)
		{
			if ((addr & 3) != 0)
			{
				throw new ReadFaultException(addr);
			}
			return unsafeMemRead(addr);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected final int unsafeMemRead(int addr) throws ReadFaultException
		protected internal int unsafeMemRead(int addr)
		{
			int page = (int)((uint)addr >> pageShift);
			int entry = (addr & (1 << pageShift) - 1)>>2;
			try
			{
				return readPages[page][entry];
			}
			catch (System.IndexOutOfRangeException e)
			{
				if (page < 0 || page >= readPages.Length)
				{
					throw new ReadFaultException(addr);
				}
				throw e; // should never happen
			}
			catch (System.NullReferenceException e)
			{
				throw new ReadFaultException(addr);
			}
		}

		/// <summary>
		/// Writes a word to the processes memory at <i>addr</i> </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void memWrite(int addr, int value) throws WriteFaultException
		public void memWrite(int addr, int value)
		{
			if ((addr & 3) != 0)
			{
				throw new WriteFaultException(addr);
			}
			unsafeMemWrite(addr,value);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected final void unsafeMemWrite(int addr, int value) throws WriteFaultException
		protected internal void unsafeMemWrite(int addr, int value)
		{
			int page = (int)((uint)addr >> pageShift);
			int entry = (addr & (1 << pageShift) - 1)>>2;
			try
			{
				writePages[page][entry] = value;
			}
			catch (System.IndexOutOfRangeException e)
			{
				if (page < 0 || page >= writePages.Length)
				{
					throw new WriteFaultException(addr);
				}
				throw e; // should never happen
			}
			catch (System.NullReferenceException e)
			{
				throw new WriteFaultException(addr);
			}
		}

		/// <summary>
		/// Created a new non-empty writable page at page number <i>page</i> </summary>
		private int[] initPage(int page)
		{
			return initPage(page,false);
		}
		/// <summary>
		/// Created a new non-empty page at page number <i>page</i>. If <i>ro</i> is set the page will be read-only </summary>
		private int[] initPage(int page, bool ro)
		{
			int[] buf = new int[(int)((uint)(1 << pageShift)>>2)];
			writePages[page] = ro ? null : buf;
			readPages[page] = buf;
			return buf;
		}

		/// <summary>
		/// Returns the exit status of the process. (only valid if state == DONE) </summary>
		///    <seealso cref= Runtime#state  </seealso>
		public int exitStatus()
		{
			if (state != EXITED)
			{
				throw new ArgumentException("exitStatus() called in an inappropriate state");
			}
			return exitStatus_Renamed;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int addStringArray(String[] strings, int topAddr) throws FaultException
		private int addStringArray(string[] strings, int topAddr)
		{
			int count = strings.Length;
			int total = 0; // null last table entry
			for (int i = 0;i < count;i++)
			{
				total += strings[i].Length + 1;
			}
			total += (count + 1) * 4;
			int start = (topAddr - total) & ~3;
			int addr = start + (count + 1) * 4;
			int[] table = new int[count + 1];
			try
			{
				for (int i = 0;i < count;i++)
				{
					sbyte[] a = getBytes(strings[i]);
					table[i] = addr;
					copyout(a,addr,a.Length);
					memset(addr + a.Length,0,1);
					addr += a.Length + 1;
				}
				addr = start;
				for (int i = 0;i < count + 1;i++)
				{
					memWrite(addr,table[i]);
					addr += 4;
				}
			}
			catch (FaultException e)
			{
				throw new Exception(e.ToString());
			}
			return start;
		}

		internal virtual string[] createEnv(string[] extra)
		{
			if (extra == null)
			{
				extra = new string[0];
			}
				return extra;
		}

		/// <summary>
		/// Sets word number <i>index</i> in the _user_info table to <i>word</i>
		/// The user_info table is a chunk of memory in the program's memory defined by the
		/// symbol "user_info". The compiler/interpreter automatically determine the size
		/// and location of the user_info table from the ELF symbol table. setUserInfo and
		/// getUserInfo are used to modify the words in the user_info table. 
		/// </summary>
		public virtual void setUserInfo(int index, int word)
		{
			if (index < 0 || index >= userInfoSize() / 4)
			{
				throw new System.IndexOutOfRangeException("setUserInfo called with index >= " + (userInfoSize() / 4));
			}
			try
			{
				memWrite(userInfoBase() + index * 4,word);
			}
			catch (FaultException e)
			{
				throw new Exception(e.ToString());
			}
		}

		/// <summary>
		/// Returns the word in the _user_info table entry <i>index</i> </summary>
		///    <seealso cref= Runtime#setUserInfo(int,int) setUserInfo  </seealso>
		public virtual int getUserInfo(int index)
		{
			if (index < 0 || index >= userInfoSize() / 4)
			{
				throw new System.IndexOutOfRangeException("setUserInfo called with index >= " + (userInfoSize() / 4));
			}
			try
			{
				return memRead(userInfoBase() + index * 4);
			}
			catch (FaultException e)
			{
				throw new Exception(e.ToString());
			}
		}

		/// <summary>
		/// Calls _execute() (subclass's execute()) and catches exceptions </summary>
		private void __execute()
		{
			try
			{
				_execute();
			}
			catch (FaultException e)
			{
				if (STDERR_DIAG)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
				exit(128 + 11,true); // SIGSEGV
				exitException = e;
			}
			catch (ExecutionException e)
			{
				if (STDERR_DIAG)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
				exit(128 + 4,true); // SIGILL
				exitException = e;
			}
		}

		/// <summary>
		/// Executes the process until the PAUSE syscall is invoked or the process exits. Returns true if the process exited. </summary>
		public bool execute()
		{
			if (state != PAUSED)
			{
				throw new ArgumentException("execute() called in inappropriate state");
			}
			if (startTime == 0)
			{
        startTime =  DateTime.UtcNow.Ticks;//System.currentTimeMillis();
			}
			state = RUNNING;
			__execute();
			if (state != PAUSED && state != EXITED && state != EXECED)
			{
        throw new ArgumentException("execute() ended up in an inappropriate state (" + state + ")");
			}
			return state != PAUSED;
		}

		internal static string[] concatArgv(string argv0, string[] rest)
		{
			string[] argv = new string[rest.Length + 1];
			Array.Copy(rest,0,argv,1,rest.Length);
			argv[0] = argv0;
			return argv;
		}

		public int run()
		{
			return run(null);
		}
		public int run(string argv0, string[] rest)
		{
			return run(concatArgv(argv0,rest));
		}
		public int run(string[] args)
		{
			return run(args,null);
		}

		/// <summary>
		/// Runs the process until it exits and returns the exit status.
		///    If the process executes the PAUSE syscall execution will be paused for 500ms and a warning will be displayed 
		/// </summary>
		public int run(string[] args, string[] env)
		{
			start(args,env);
			for (;;)
			{
				if (execute())
				{
					break;
				}
				if (STDERR_DIAG)
				{
					Console.Error.WriteLine("WARNING: Pause requested while executing run()");
				}
			}
			if (state == EXECED && STDERR_DIAG)
			{
				Console.Error.WriteLine("WARNING: Process exec()ed while being run under run()");
			}
			return state == EXITED ? exitStatus() : 0;
		}

		public void start()
		{
			start(null);
		}
		public void start(string[] args)
		{
			start(args,null);
		}

		/// <summary>
		/// Initializes the process and prepairs it to be executed with execute() </summary>
		public void start(string[] args, string[] environ)
		{
			int top, sp, argsAddr, envAddr;
			if (state != STOPPED)
			{
        throw new ArgumentException("start() called in inappropriate state");
			}
			if (args == null)
			{
				args = new string[]{this.GetType().Name};
			}

			sp = top = writePages.Length * (1 << pageShift);
			try
			{
				sp = argsAddr = addStringArray(args,sp);
				sp = envAddr = addStringArray(createEnv(environ),sp);
			}
			catch (FaultException e)
			{
				throw new System.ArgumentException("args/environ too big");
			}
			sp &= ~15;
			if (top - sp > ARG_MAX)
			{
				throw new System.ArgumentException("args/environ too big");
			}

			// HACK: heapStart() isn't always available when the constructor
			// is run and this sometimes doesn't get initialized
			if (heapEnd == 0)
			{
				heapEnd = heapStart();
				if (heapEnd == 0)
				{
					throw new Exception("heapEnd == 0");
				}
				int pageSize = writePages.Length == 1 ? 4096 : (1 << pageShift);
				heapEnd = (heapEnd + pageSize - 1) & ~(pageSize-1);
			}

			CpuState cpuState = new CpuState();
			cpuState.r[A0] = argsAddr;
			cpuState.r[A1] = envAddr;
			cpuState.r[SP] = sp;
			cpuState.r[RA] = unchecked((int)0xdeadbeef);
			cpuState.r[GP] = gp();
			cpuState.pc = entryPoint();
			CPUState = cpuState;

			state = PAUSED;

			_started();
		}

		public void stop()
		{
			if (state != RUNNING && state != PAUSED)
			{
        throw new ArgumentException("stop() called in inappropriate state");
			}
			exit(0, false);
		}

		/// <summary>
		/// Hook for subclasses to do their own startup </summary>
		internal virtual void _started()
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int call(String sym, Object[] args) throws CallException, FaultException
		public int call(string sym, object[] args)
		{
      throw new NotImplementedException();
      /*
			if (state != PAUSED && state != CALLJAVA)
			{
        throw new ArgumentException("call() called in inappropriate state");
			}
			if (args.Length > 7)
			{
				throw new System.ArgumentException("args.length > 7");
			}
			CpuState state = new CpuState();
			getCPUState(state);

			int sp = state.r[SP];
			int[] ia = new int[args.Length];
			for (int i = 0;i < args.Length;i++)
			{
				object o = args[i];
				sbyte[] buf = null;
				if (o is string)
				{
					buf = getBytes((string)o);
				}
				else if (o is sbyte[])
				{
					buf = (sbyte[]) o;
				}
				else if (o is Number)
				{
					ia[i] = (int)((Number)o);
				}
				if (buf != null)
				{
					sp -= buf.Length;
					copyout(buf,sp,buf.Length);
					ia[i] = sp;
				}
			}
			int oldSP = state.r[SP];
			if (oldSP == sp)
			{
				return call(sym,ia);
			}

			state.r[SP] = sp;
			CPUState = state;
			int ret = call(sym,ia);
			state.r[SP] = oldSP;
			CPUState = state;
			return ret;
      */
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int call(String sym) throws CallException
		public int call(string sym)
		{
			return call(sym,new int[]{});
		}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int call(String sym, int a0) throws CallException
		public int call(string sym, int a0)
		{
			return call(sym,new int[]{a0});
		}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int call(String sym, int a0, int a1) throws CallException
		public int call(string sym, int a0, int a1)
		{
			return call(sym,new int[]{a0,a1});
		}

		/// <summary>
		/// Calls a function in the process with the given arguments </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int call(String sym, int[] args) throws CallException
		public int call(string sym, int[] args)
		{
			int func = lookupSymbol(sym);
			if (func == -1)
			{
				throw new CallException(sym + " not found");
			}
			int helper = lookupSymbol("_call_helper");
			if (helper == -1)
			{
				throw new CallException("_call_helper not found");
			}
			return call(helper,func,args);
		}

		/// <summary>
		/// Executes the code at <i>addr</i> in the process setting A0-A3 and S0-S3 to the given arguments
		///    and returns the contents of V1 when the the pause syscall is invoked 
		/// </summary>
		//public final int call(int addr, int a0, int a1, int a2, int a3, int s0, int s1, int s2, int s3) {
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int call(int addr, int a0, int[] rest) throws CallException
		public int call(int addr, int a0, int[] rest)
		{
			if (rest.Length > 7)
			{
				throw new System.ArgumentException("rest.length > 7");
			}
			if (state != PAUSED && state != CALLJAVA)
			{
        throw new ArgumentException("call() called in inappropriate state");
			}
			int oldState = state;
			CpuState saved = new CpuState();
			getCPUState(saved);
			CpuState cpustate = saved.dup();

			cpustate.r[SP] = cpustate.r[SP] & ~15;
			cpustate.r[RA] = unchecked((int)0xdeadbeef);
			cpustate.r[A0] = a0;
			switch (rest.Length)
			{
				case 7:
					cpustate.r[S3] = rest[6];
					goto case 6;
				case 6:
					cpustate.r[S2] = rest[5];
					goto case 5;
				case 5:
					cpustate.r[S1] = rest[4];
					goto case 4;
				case 4:
					cpustate.r[S0] = rest[3];
					goto case 3;
				case 3:
					cpustate.r[A3] = rest[2];
					goto case 2;
				case 2:
					cpustate.r[A2] = rest[1];
					goto case 1;
				case 1:
					cpustate.r[A1] = rest[0];
				break;
			}
			cpustate.pc = addr;

			state = RUNNING;

			CPUState = cpustate;
			__execute();
			getCPUState(cpustate);
			CPUState = saved;

			if (state != PAUSED)
			{
				throw new CallException("Process exit()ed while servicing a call() request");
			}
			state = oldState;

			return cpustate.r[V1];
		}

		/// <summary>
		/// Allocated an entry in the FileDescriptor table for <i>fd</i> and returns the number.
		///    Returns -1 if the table is full. This can be used by subclasses to use custom file
		///    descriptors 
		/// </summary>
		public int addFD(FD fd)
		{
			if (state == EXITED || state == EXECED)
			{
        throw new ArgumentException("addFD called in inappropriate state");
			}
			int i;
			for (i = 0;i < OPEN_MAX;i++)
			{
				if (fds[i] == null)
				{
					break;
				}
			}
			if (i == OPEN_MAX)
			{
				return -1;
			}
			fds[i] = fd;
			closeOnExec[i] = false;
			return i;
		}

		/// <summary>
		/// Hooks for subclasses before and after the process closes an FD </summary>
		internal virtual void _preCloseFD(FD fd)
		{
		}
		internal virtual void _postCloseFD(FD fd)
		{
		}

		/// <summary>
		/// Closes file descriptor <i>fdn</i> and removes it from the file descriptor table </summary>
		public bool closeFD(int fdn)
		{
			if (state == EXITED || state == EXECED)
			{
        throw new ArgumentException("closeFD called in inappropriate state");
			}
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return false;
			}
			if (fds[fdn] == null)
			{
				return false;
			}
			_preCloseFD(fds[fdn]);
			fds[fdn].close();
			_postCloseFD(fds[fdn]);
			fds[fdn] = null;
			return true;
		}

		/// <summary>
		/// Duplicates the file descriptor <i>fdn</i> and returns the new fs </summary>
		public int dupFD(int fdn)
		{
			int i;
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -1;
			}
			if (fds[fdn] == null)
			{
				return -1;
			}
			for (i = 0;i < OPEN_MAX;i++)
			{
				if (fds[i] == null)
				{
					break;
				}
			}
			if (i == OPEN_MAX)
			{
				return -1;
			}
			fds[i] = fds[fdn].dup();
			return i;
		}

		public const int RD_ONLY = 0;
		public const int WR_ONLY = 1;
		public const int RDWR = 2;

		public const int O_CREAT = 0x0200;
		public const int O_EXCL = 0x0800;
		public const int O_APPEND = 0x0008;
		public const int O_TRUNC = 0x0400;
		public const int O_NONBLOCK = 0x4000;
		public const int O_NOCTTY = 0x8000;


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: FD hostFSOpen(final File f, int flags, int mode, final Object data) throws ErrnoException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		internal virtual FD hostFSOpen(File f, int flags, int mode, object data)
		{
			if ((flags & ~(3 | O_CREAT | O_EXCL | O_APPEND | O_TRUNC)) != 0)
			{
				if (STDERR_DIAG)
				{
					Console.Error.WriteLine("WARNING: Unsupported flags passed to open(\"" + f + "\"): " + toHex(flags & ~(3 | O_CREAT | O_EXCL | O_APPEND | O_TRUNC)));
				}
				throw new ErrnoException(ENOTSUP);
			}
			bool write = (flags & 3) != RD_ONLY;

			if (sm != null && !(write ? sm.allowWrite(f) : sm.allowRead(f)))
			{
				throw new ErrnoException(EACCES);
			}

			if ((flags & (O_EXCL | O_CREAT)) == (O_EXCL | O_CREAT))
			{
				try
				{
					if (!Platform.atomicCreateFile(f))
					{
						throw new ErrnoException(EEXIST);
					}
				}
				catch (IOException e)
				{
					throw new ErrnoException(EIO);
				}
			}
      else throw new NotImplementedException();
      /*
			else if (!f.exists())
			{
				if ((flags & O_CREAT) == 0)
				{
					return null;
				}
			}
			else if (f.Directory)
			{
				return hostFSDirFD(f,data);
			}
      */

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Seekable.File sf;
			File sf;
			try
			{
				sf = new File(f,write,(flags & O_TRUNC) != 0);
			}
			catch (FileNotFoundException e)
			{
				if (e.Message != null && e.Message.IndexOf("Permission denied") >= 0)
				{
					throw new ErrnoException(EACCES);
				}
				return null;
			}
			catch (IOException e)
			{
				throw new ErrnoException(EIO);
			}

			return new SeekableFdAnonymousInnerClassHelper(this, sf, flags, f, data);
		}


		internal virtual FStat hostFStat(File f, File sf, object data)
		{
			return new HostFStat(f,sf);
		}

		internal virtual FD hostFSDirFD(File f, object data)
		{
			return null;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: FD _open(String path, int flags, int mode) throws ErrnoException
		internal virtual FD _open(string path, int flags, int mode)
		{
			return hostFSOpen(new File(path),flags,mode,null);
		}

		/// <summary>
		/// The open syscall </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_open(int addr, int flags, int mode) throws ErrnoException, FaultException
		private int sys_open(int addr, int flags, int mode)
		{
			string name = cstring(addr);

			// HACK: TeX, or GPC, or something really sucks
			if (name.Length == 1024 && this.GetType().Name.Equals("tests.TeX"))
			{
				name = name.Trim();
			}

			flags &= ~O_NOCTTY; // this is meaningless under nestedvm
			FD fd = _open(name,flags,mode);
			if (fd == null)
			{
				return -ENOENT;
			}
			int fdn = addFD(fd);
			if (fdn == -1)
			{
				fd.close();
				return -ENFILE;
			}
			return fdn;
		}

		/// <summary>
		/// The write syscall </summary>

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_write(int fdn, int addr, int count) throws FaultException, ErrnoException
		private int sys_write(int fdn, int addr, int count)
		{
			count = Math.Min(count,MAX_CHUNK);
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[fdn] == null)
			{
				return -EBADFD;
			}
			sbyte[] buf = byteBuf(count);
			copyin(addr,buf,count);
			try
			{
				return fds[fdn].write(buf,0,count);
			}
			catch (ErrnoException e)
			{
				if (e.errno == EPIPE)
				{
					sys_exit(128 + 13);
				}
				throw e;
			}
		}

		/// <summary>
		/// The read syscall </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_read(int fdn, int addr, int count) throws FaultException, ErrnoException
		private int sys_read(int fdn, int addr, int count)
		{
			count = Math.Min(count,MAX_CHUNK);
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[fdn] == null)
			{
				return -EBADFD;
			}
			sbyte[] buf = byteBuf(count);
			int n = fds[fdn].read(buf,0,count);
			copyout(buf,addr,n);
			return n;
		}

		/// <summary>
		/// The ftruncate syscall </summary>
		private int sys_ftruncate(int fdn, long length)
		{
		  if (fdn < 0 || fdn >= OPEN_MAX)
		  {
			  return -EBADFD;
		  }
		  if (fds[fdn] == null)
		  {
			  return -EBADFD;
		  }

		  Seekable seekable = fds[fdn].seekable();
		  if (length < 0 || seekable == null)
		  {
			  return -EINVAL;
		  }
		  try
		  {
			  seekable.resize(length);
		  }
		  catch (IOException e)
		  {
			  return -EIO;
		  }
		  return 0;
		}

		/// <summary>
		/// The close syscall </summary>
		private int sys_close(int fdn)
		{
			return closeFD(fdn) ? 0 : -EBADFD;
		}


		/// <summary>
		/// The seek syscall </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_lseek(int fdn, int offset, int whence) throws ErrnoException
		private int sys_lseek(int fdn, int offset, int whence)
		{
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[fdn] == null)
			{
				return -EBADFD;
			}
			if (whence != SEEK_SET && whence != SEEK_CUR && whence != SEEK_END)
			{
				return -EINVAL;
			}
			int n = fds[fdn].seek(offset,whence);
			return n < 0 ? - ESPIPE : n;
		}

		/// <summary>
		/// The stat/fstat syscall helper </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: int stat(FStat fs, int addr) throws FaultException
		internal virtual int stat(FStat fs, int addr)
		{
			memWrite(addr + 0,(fs.dev() << 16) | (fs.inode() & 0xffff)); // st_dev (top 16), // st_ino (bottom 16)
			memWrite(addr + 4,((fs.type() & 0xf000)) | (fs.mode() & 0xfff)); // st_mode
			memWrite(addr + 8,fs.nlink() << 16 | fs.uid() & 0xffff); // st_nlink (top 16) // st_uid (bottom 16)
			memWrite(addr + 12,fs.gid() << 16 | 0); // st_gid (top 16) // st_rdev (bottom 16)
			memWrite(addr + 16,fs.size()); // st_size
			memWrite(addr + 20,fs.atime()); // st_atime
			// memWrite(addr+24,0) // st_spare1
			memWrite(addr + 28,fs.mtime()); // st_mtime
			// memWrite(addr+32,0) // st_spare2
			memWrite(addr + 36,fs.ctime()); // st_ctime
			// memWrite(addr+40,0) // st_spare3
			memWrite(addr + 44,fs.blksize()); // st_bklsize;
			memWrite(addr + 48,fs.blocks()); // st_blocks
			// memWrite(addr+52,0) // st_spare4[0]
			// memWrite(addr+56,0) // st_spare4[1]
			return 0;
		}

		/// <summary>
		/// The fstat syscall </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_fstat(int fdn, int addr) throws FaultException
		private int sys_fstat(int fdn, int addr)
		{
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[fdn] == null)
			{
				return -EBADFD;
			}
			return stat(fds[fdn].fstat(),addr);
		}

		/*
		struct timeval {
		long tv_sec;
		long tv_usec;
		};
		*/
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_gettimeofday(int timevalAddr, int timezoneAddr) throws FaultException
		private int sys_gettimeofday(int timevalAddr, int timezoneAddr)
		{
      throw new NotImplementedException();
      long now = 0; //System.currentTimeMillis();
			int tv_sec = (int)(now / 1000);
			int tv_usec = (int)((now % 1000) * 1000);
			memWrite(timevalAddr + 0,tv_sec);
			memWrite(timevalAddr + 4,tv_usec);
			return 0;
		}

		private int sys_sleep(int sec)
		{
			if (sec < 0)
			{
				sec = int.MaxValue;
			}
			try
			{
				Thread.Sleep(sec * 1000);
				return 0;
			}
			catch (ThreadInterruptedException e)
			{
				return -1;
			}
		}

		/*
		  #define _CLOCKS_PER_SEC_ 1000
		  #define    _CLOCK_T_    unsigned long
		struct tms {
		  clock_t   tms_utime;
		  clock_t   tms_stime;
		  clock_t   tms_cutime;    
		  clock_t   tms_cstime;
		};*/

		private int sys_times(int tms)
		{
      throw new NotImplementedException();
      long now = 0;//System.currentTimeMillis();
			int userTime = (int)((now - startTime) / 16);
			int sysTime = (int)((now - startTime) / 16);

			try
			{
				if (tms != 0)
				{
					memWrite(tms + 0,userTime);
					memWrite(tms + 4,sysTime);
					memWrite(tms + 8,userTime);
					memWrite(tms + 12,sysTime);
				}
			}
			catch (FaultException e)
			{
				return -EFAULT;
			}
			return (int)now;
		}

		private int sys_sysconf(int n)
		{
			switch (n)
			{
				case _SC_CLK_TCK:
					return 1000;
				case _SC_PAGESIZE:
					return writePages.Length == 1 ? 4096 : (1 << pageShift);
				case _SC_PHYS_PAGES:
					return writePages.Length == 1 ? (1 << pageShift) / 4096 : writePages.Length;
				default:
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("WARNING: Attempted to use unknown sysconf key: " + n);
					}
					return -EINVAL;
			}
		}

		/// <summary>
		/// The sbrk syscall. This can also be used by subclasses to allocate memory.
		///    <i>incr</i> is how much to increase the break by 
		/// </summary>
		public int sbrk(int incr)
		{
			if (incr < 0)
			{
				return -ENOMEM;
			}
			if (incr == 0)
			{
				return heapEnd;
			}
			incr = (incr + 3) & ~3;
			int oldEnd = heapEnd;
			int newEnd = oldEnd + incr;
			if (newEnd >= stackBottom)
			{
				return -ENOMEM;
			}

			if (writePages.Length > 1)
			{
				int pageMask = (1 << pageShift) - 1;
				int pageWords = (int)((uint)(1 << pageShift) >> 2);
				int start = (int)((uint)(oldEnd + pageMask) >> pageShift);
				int end = (int)((uint)(newEnd + pageMask) >> pageShift);
				try
				{
					for (int i = start;i < end;i++)
					{
						readPages[i] = writePages[i] = new int[pageWords];
					}
				}
				catch (System.OutOfMemoryException e)
				{
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("WARNING: Caught OOM Exception in sbrk: " + e);
					}
					return -ENOMEM;
				}
			}
			heapEnd = newEnd;
			return oldEnd;
		}

		/// <summary>
		/// The getpid syscall </summary>
		private int sys_getpid()
		{
			return Pid;
		}
		internal virtual int Pid
		{
			get
			{
				return 1;
			}
		}

		public interface ICallJavaCB
		{
			int call(int a, int b, int c, int d);
		}

		private int sys_calljava(int a, int b, int c, int d)
		{
			if (state != RUNNING)
			{
				throw new ArgumentException("wound up calling sys_calljava while not in RUNNING");
			}
			if (callJavaCB != null)
			{
				state = CALLJAVA;
				int ret;
				try
				{
					ret = callJavaCB.call(a,b,c,d);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Error while executing callJavaCB");
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
					ret = 0;
				}
				state = RUNNING;
				return ret;
			}
			else
			{
				if (STDERR_DIAG)
				{
					Console.Error.WriteLine("WARNING: calljava syscall invoked without a calljava callback set");
				}
				return 0;
			}
		}

		private int sys_pause()
		{
			state = PAUSED;
			return 0;
		}

		private int sys_getpagesize()
		{
			return writePages.Length == 1 ? 4096 : (1 << pageShift);
		}

		/// <summary>
		/// Hook for subclasses to do something when the process exits </summary>
		internal virtual void _exited()
		{
		}

		internal virtual void exit(int status, bool fromSignal)
		{
			if (fromSignal && fds[2] != null)
			{
				try
				{
					sbyte[] msg = getBytes("Process exited on signal " + (status - 128) + "\n");
					fds[2].write(msg,0,msg.Length);
				}
				catch (ErrnoException e)
				{
				}
			}
			exitStatus_Renamed = status;
			for (int i = 0;i < fds.Length;i++)
			{
				if (fds[i] != null)
				{
					closeFD(i);
				}
			}
			state = EXITED;
			_exited();
		}

		private int sys_exit(int status)
		{
			exit(status,false);
			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: final int sys_fcntl(int fdn, int cmd, int arg) throws FaultException
		internal int sys_fcntl(int fdn, int cmd, int arg)
		{
			int i;

			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[fdn] == null)
			{
				return -EBADFD;
			}
			FD fd = fds[fdn];

			switch (cmd)
			{
				case F_DUPFD:
					if (arg < 0 || arg >= OPEN_MAX)
					{
						return -EINVAL;
					}
					for (i = arg;i < OPEN_MAX;i++)
					{
						if (fds[i] == null)
						{
							break;
						}
					}
					if (i == OPEN_MAX)
					{
						return -EMFILE;
					}
					fds[i] = fd.dup();
					return i;
				case F_GETFL:
					return fd.flags();
				case F_SETFD:
					closeOnExec[fdn] = arg != 0;
					return 0;
				case F_GETFD:
					return closeOnExec[fdn] ? 1 : 0;
				case F_GETLK:
				case F_SETLK:
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("WARNING: file locking requires UnixRuntime");
					}
					return -ENOSYS;
				default:
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("WARNING: Unknown fcntl command: " + cmd);
					}
					return -ENOSYS;
			}
		}

		internal int fsync(int fdn)
		{
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[fdn] == null)
			{
				return -EBADFD;
			}
			FD fd = fds[fdn];

			Seekable s = fd.seekable();
			if (s == null)
			{
				return -EINVAL;
			}

			try
			{
				s.sync();
				return 0;
			}
			catch (IOException e)
			{
				return -EIO;
			}
		}

		/// <summary>
		/// The syscall dispatcher.
		///    The should be called by subclasses when the syscall instruction is invoked.
		///    <i>syscall</i> should be the contents of V0 and <i>a</i>, <i>b</i>, <i>c</i>, and <i>d</i> should be 
		///    the contenst of A0, A1, A2, and A3. The call MAY change the state </summary>
		///    <seealso cref= Runtime#state state  </seealso>
		protected internal int syscall(int syscall, int a, int b, int c, int d, int e, int f)
		{
			try
			{
				int n = _syscall(syscall,a,b,c,d,e,f);
				//if(n<0) throw new ErrnoException(-n);
				return n;
			}
			catch (ErrnoException ex)
			{
				//System.err.println("While executing syscall: " + syscall + ":");
				//if(syscall == SYS_open) try { System.err.println("Failed to open " + cstring(a) + " errno " + ex.errno); } catch(Exception e2) { }
				//ex.printStackTrace();
				return -ex.errno;
			}
			catch (FaultException ex)
			{
				return -EFAULT;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.Write(ex.StackTrace);
				throw new Exception("Internal Error in _syscall()");
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: int _syscall(int syscall, int a, int b, int c, int d, int e, int f) throws ErrnoException, FaultException
		internal virtual int _syscall(int syscall, int a, int b, int c, int d, int e, int f)
		{
			switch (syscall)
			{
				case SYS_null:
					return 0;
				case SYS_exit:
					return sys_exit(a);
				case SYS_pause:
					return sys_pause();
				case SYS_write:
					return sys_write(a,b,c);
				case SYS_fstat:
					return sys_fstat(a,b);
				case SYS_sbrk:
					return sbrk(a);
				case SYS_open:
					return sys_open(a,b,c);
				case SYS_close:
					return sys_close(a);
				case SYS_read:
					return sys_read(a,b,c);
				case SYS_lseek:
					return sys_lseek(a,b,c);
				case SYS_ftruncate:
					return sys_ftruncate(a,b);
				case SYS_getpid:
					return sys_getpid();
				case SYS_calljava:
					return sys_calljava(a,b,c,d);
				case SYS_gettimeofday:
					return sys_gettimeofday(a,b);
				case SYS_sleep:
					return sys_sleep(a);
				case SYS_times:
					return sys_times(a);
				case SYS_getpagesize:
					return sys_getpagesize();
				case SYS_fcntl:
					return sys_fcntl(a,b,c);
				case SYS_sysconf:
					return sys_sysconf(a);
				case SYS_getuid:
					return sys_getuid();
				case SYS_geteuid:
					return sys_geteuid();
				case SYS_getgid:
					return sys_getgid();
				case SYS_getegid:
					return sys_getegid();

				case SYS_fsync:
					return fsync(a);
				case SYS_memcpy:
					memcpy(a,b,c);
					return a;
				case SYS_memset:
					memset(a,b,c);
					return a;

				case SYS_kill:
				case SYS_fork:
				case SYS_pipe:
				case SYS_dup2:
				case SYS_waitpid:
				case SYS_stat:
				case SYS_mkdir:
				case SYS_getcwd:
				case SYS_chdir:
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("Attempted to use a UnixRuntime syscall in Runtime (" + syscall + ")");
					}
					return -ENOSYS;
				default:
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("Attempted to use unknown syscall: " + syscall);
					}
					return -ENOSYS;
			}
		}

		private int sys_getuid()
		{
			return 0;
		}
		private int sys_geteuid()
		{
			return 0;
		}
		private int sys_getgid()
		{
			return 0;
		}
		private int sys_getegid()
		{
			return 0;
		}

		public virtual int xmalloc(int size)
		{
			int p = malloc(size);
			if (p == 0)
			{
				throw new Exception("malloc() failed");
			}
				return p;
		}
		public virtual int xrealloc(int addr, int newsize)
		{
			int p = realloc(addr,newsize);
			if (p == 0)
			{
				throw new Exception("realloc() failed");
			}
				return p;
		}
		public virtual int realloc(int addr, int newsize)
		{
			try
			{
				return call("realloc",addr,newsize);
			}
			catch (CallException e)
			{
				return 0;
			}
		}
		public virtual int malloc(int size)
		{
			try
			{
				return call("malloc",size);
			}
			catch (CallException e)
			{
				return 0;
			}
		}
		public virtual void free(int p) //noop
		{
			try
			{
				if (p != 0)
				{
					call("free",p);
				}
			}
			catch (CallException e)
			{
			}
		}

		/// <summary>
		/// Helper function to create a cstring in main memory </summary>
		public virtual int strdup(string s)
		{
			sbyte[] a;
			if (s == null)
			{
				s = "(null)";
			}
			sbyte[] a2 = getBytes(s);
			a = new sbyte[a2.Length + 1];
			Array.Copy(a2,0,a,0,a2.Length);
			int addr = malloc(a.Length);
			if (addr == 0)
			{
				return 0;
			}
			try
			{
				copyout(a,addr,a.Length);
			}
			catch (FaultException e)
			{
				free(addr);
				return 0;
			}
			return addr;
		}

		// TODO: less memory copying (custom utf-8 reader)
		//       or at least roll strlen() into copyin()
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final String utfstring(int addr) throws ReadFaultException
		public string utfstring(int addr)
		{
			if (addr == 0)
			{
				return null;
			}

			// determine length
			int i = addr;
			for (int word = 1; word != 0; i++)
			{
				word = memRead(i & ~3);
				switch (i & 3)
				{
					case 0:
						word = ((int)((uint)word >> 24)) & 0xff;
						break;
					case 1:
						word = ((int)((uint)word >> 16)) & 0xff;
						break;
					case 2:
						word = ((int)((uint)word >> 8)) & 0xff;
						break;
					case 3:
						word = ((int)((uint)word >> 0)) & 0xff;
						break;
				}
			}
			if (i > addr) // do not count null
			{
				i--;
			}

			byte[] bytes = new byte[i - addr];
			copyin(addr, bytes, bytes.Length);

			
        return Encoding.UTF8.GetString(bytes); //new string(bytes, "UTF-8");
			
		}

		/// <summary>
		/// Helper function to read a cstring from main memory </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final String cstring(int addr) throws ReadFaultException
		public string cstring(int addr)
		{
			if (addr == 0)
			{
				return null;
			}
			StringBuilder sb = new StringBuilder();
			for (;;)
			{
				int word = memRead(addr & ~3);
				switch (addr & 3)
				{
					case 0:
						if ((((int)((uint)word >> 24)) & 0xff) == 0)
						{
							return sb.ToString();
						}
							sb.Append((char)(((int)((uint)word >> 24)) & 0xff));
							addr++;
						goto case 1;
					case 1:
						if ((((int)((uint)word >> 16)) & 0xff) == 0)
						{
							return sb.ToString();
						}
							sb.Append((char)(((int)((uint)word >> 16)) & 0xff));
							addr++;
						goto case 2;
					case 2:
						if ((((int)((uint)word >> 8)) & 0xff) == 0)
						{
							return sb.ToString();
						}
							sb.Append((char)(((int)((uint)word >> 8)) & 0xff));
							addr++;
						goto case 3;
					case 3:
						if ((((int)((uint)word >> 0)) & 0xff) == 0)
						{
							return sb.ToString();
						}
							sb.Append((char)(((int)((uint)word >> 0)) & 0xff));
							addr++;
						break;
				}
			}
		}


		// Null pointer check helper function
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected final void nullPointerCheck(int addr) throws ExecutionException
		protected internal void nullPointerCheck(int addr)
		{
			if (addr < 65536)
			{
				throw new ExecutionException("Attempted to dereference a null pointer " + toHex(addr));
			}
		}

		// Utility functions
		internal virtual sbyte[] byteBuf(int size)
		{
			if (_byteBuf == null)
			{
				_byteBuf = new sbyte[size];
			}
			else if (_byteBuf.Length < size)
			{
				_byteBuf = new sbyte[min(max(_byteBuf.Length * 2,size),MAX_CHUNK)];
			}
			return _byteBuf;
		}

		/// <summary>
		/// Decode a packed string </summary>
		protected internal static int[] decodeData(string s, int words)
		{
			if (s.Length % 8 != 0)
			{
				throw new System.ArgumentException("string length must be a multiple of 8");
			}
			if ((s.Length / 8) * 7 < words * 4)
			{
				throw new System.ArgumentException("string isn't big enough");
			}
			int[] buf = new int[words];
			int prev = 0, left = 0;
			for (int i = 0,n = 0;n < words;i += 8)
			{
				long l = 0;
				for (int j = 0;j < 8;j++)
				{
					l <<= 7;
					l |= s[i + j] & 0x7f;
				}
				if (left > 0)
				{
					buf[n++] = prev | (int)((long)((ulong)l >> (56 - left)));
				}
				if (n < words)
				{
					buf[n++] = (int)((long)((ulong)l >> (24 - left)));
				}
				left = (left + 8) & 0x1f;
				prev = (int)(l << left);
			}
			return buf;
		}

		internal static sbyte[] getBytes(string s)
		{
      byte[] foo = Encoding.UTF8.GetBytes(s);
      sbyte[] bar = new sbyte[foo.Length];
      Array.ConstrainedCopy(foo,0,bar,0,foo.Length);
      return bar;
		}

		internal static sbyte[] getNullTerminatedBytes(string s)
		{
			sbyte[] buf1 = getBytes(s);
			sbyte[] buf2 = new sbyte[buf1.Length + 1];
			Array.Copy(buf1,0,buf2,0,buf1.Length);
			return buf2;
		}

		internal static string toHex(int n)
		{
			return "0x" + Convert.ToString(n & 0xffffffffL, 16);
		}
		internal static int min(int a, int b)
		{
			return a < b ? a : b;
		}
		internal static int max(int a, int b)
		{
			return a > b ? a : b;
		}
	}



  /// <summary>
  /// File Descriptor class </summary>
  public abstract class FD
  {
      internal int refCount = 1;
      internal string normalizedPath = null;
      internal bool deleteOnClose = false;

      public virtual string NormalizedPath
      {
          set
          {
              normalizedPath = value;
          }
          get
          {
              return normalizedPath;
          }
      }

      public virtual void markDeleteOnClose()
      {
          deleteOnClose = true;
      }
      public virtual bool MarkedForDeleteOnClose
      {
          get
          {
              return deleteOnClose;
          }
      }

      /// <summary>
      /// Read some bytes. Should return the number of bytes read, 0 on EOF, or throw an IOException on error </summary>
      //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
      //ORIGINAL LINE: public int read(byte[] a, int off, int length) throws ErrnoException
      public virtual int read(sbyte[] a, int off, int length)
      {
          throw new ErrnoException(EBADFD);
      }
      /// <summary>
      /// Write. Should return the number of bytes written or throw an IOException on error </summary>
      //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
      //ORIGINAL LINE: public int write(byte[] a, int off, int length) throws ErrnoException
      public virtual int write(sbyte[] a, int off, int length)
      {
          throw new ErrnoException(EBADFD);
      }

      /// <summary>
      /// Seek in the filedescriptor. Whence is SEEK_SET, SEEK_CUR, or SEEK_END. Should return -1 on error or the new position. </summary>
      //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
      //ORIGINAL LINE: public int seek(int n, int whence) throws ErrnoException
      public virtual int seek(int n, int whence)
      {
          return -1;
      }

      //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
      //ORIGINAL LINE: public int getdents(byte[] a, int off, int length) throws ErrnoException
      public virtual int getdents(sbyte[] a, int off, int length)
      {
          throw new ErrnoException(EBADFD);
      }

      /// <summary>
      /// Return a Seekable object representing this file descriptor (can be read only) 
      ///    This is required for exec() 
      /// </summary>
      internal virtual Seekable seekable()
      {
          return null;
      }

      internal FStat cachedFStat = null;
      public FStat fstat()
      {
          if (cachedFStat == null)
          {
              cachedFStat = _fstat();
          }
          return cachedFStat;
      }

      protected internal abstract FStat _fstat();
      public abstract int flags();

      /// <summary>
      /// Closes the fd </summary>
      public void close()
      {
          if (--refCount == 0)
          {
              _close();
          }
      }
      protected internal virtual void _close() // noop
      {
      }

      internal virtual FD dup()
      {
          refCount++;
          return this;
      }
  }

    // This is pretty inefficient but it is only used for reading from the console on win32

    // Exceptions


    // CPU State
}