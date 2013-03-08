using System;
using System.Collections;
using System.Text;
using System.Threading;

// Copyright 2000-2005 the Contributors, as shown in the revision logs.
// Licensed under the Apache License 2.0 ("the License").
// You may not use this file except in compliance with the License.
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace org.ibex.nestedvm
{

	using org.ibex.nestedvm.util;

	// FEATURE: vfork

	public abstract class UnixRuntime : Runtime //, ICloneable
	{
		/// <summary>
		/// The pid of this "process" </summary>
		private int pid;
		private UnixRuntime parent;
		public int Pid
		{
			get
			{
				return pid;
			}
		}

		private static readonly GlobalState defaultGS = new GlobalState();
		private GlobalState gs;
		public virtual GlobalState GlobalState
		{
			set
			{
				if (state != STOPPED)
				{
					throw new ArgumentException("can't change GlobalState when running");
				}
				if (value == null)
				{
					throw new System.NullReferenceException("gs is null");
				}
				this.gs = value;
			}
		}

		/// <summary>
		/// proceses' current working directory - absolute path WITHOUT leading slash
		///    "" = root, "bin" = /bin "usr/bin" = /usr/bin 
		/// </summary>
		private string cwd;

		/// <summary>
		/// The runtime that should be run next when in state == EXECED </summary>
		private UnixRuntime execedRuntime;

		private object children; // used only for synchronizatin
		private ArrayList activeChildren;
		private ArrayList exitedChildren;

		protected internal UnixRuntime(int pageSize, int totalPages) : this(pageSize,totalPages,false)
		{
		}
		protected internal UnixRuntime(int pageSize, int totalPages, bool exec) : base(pageSize,totalPages,exec)
		{

			if (!exec)
			{
				gs = defaultGS;
				string userdir = Platform.getProperty("user.dir");
				cwd = userdir == null ? null : gs.mapHostPath(userdir);
				if (cwd == null)
				{
					cwd = "/";
				}
				cwd = cwd.Substring(1);
			}
		}

		private static string posixTZ()
		{
			StringBuilder sb = new StringBuilder();
      TimeZone zone = TimeZone.CurrentTimeZone;//TimeZone.Default;
      int off = (int)zone.GetUtcOffset(DateTime.Now).TotalSeconds;// Rawoffset/ 1000;
			sb.Append(Platform.timeZoneGetDisplayName(zone,false,false));
			if (off > 0)
			{
				sb.Append("-");
			}
			else
			{
				off = -off;
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
      //if (zone.useDaylightTime())
      // probably wrong.
      if (zone.GetDaylightChanges(DateTime.Now.Year).Delta.TotalSeconds > 0)
      {
				sb.Append(Platform.timeZoneGetDisplayName(zone,true,false));
			}
			return sb.ToString();
		}

		private static bool envHas(string key, string[] environ)
		{
			for (int i = 0;i < environ.Length;i++)
			{
				if (environ[i] != null && environ[i].StartsWith(key + "="))
				{
					return true;
				}
			}
			return false;
		}

		internal virtual string[] createEnv(string[] extra)
		{
			string[] defaults = new string[7];
			int n = 0;
			if (extra == null)
			{
				extra = new string[0];
			}
			string tmp;
			if (!envHas("USER",extra) && Platform.getProperty("user.name") != null)
			{
				defaults[n++] = "USER=" + Platform.getProperty("user.name");
			}
			if (!envHas("HOME",extra) && (tmp = Platform.getProperty("user.home")) != null && (tmp = gs.mapHostPath(tmp)) != null)
			{
				defaults[n++] = "HOME=" + tmp;
			}
			if (!envHas("TMPDIR",extra) && (tmp = Platform.getProperty("java.io.tmpdir")) != null && (tmp = gs.mapHostPath(tmp)) != null)
			{
				defaults[n++] = "TMPDIR=" + tmp;
			}
			if (!envHas("SHELL",extra))
			{
				defaults[n++] = "SHELL=/bin/sh";
			}
			if (!envHas("TERM",extra) && !win32Hacks)
			{
				defaults[n++] = "TERM=vt100";
			}
			if (!envHas("TZ",extra))
			{
				defaults[n++] = "TZ=" + posixTZ();
			}
			if (!envHas("PATH",extra))
			{
				defaults[n++] = "PATH=/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin";
			}
			string[] env = new string[extra.Length + n];
			for (int i = 0;i < n;i++)
			{
				env[i] = defaults[i];
			}
			for (int i = 0;i < extra.Length;i++)
			{
				env[n++] = extra[i];
			}
			return env;
		}

		private class ProcessTableFullExn : Exception
		{
		}

		internal virtual void _started()
		{
			UnixRuntime[] tasks = gs.tasks;
			lock (gs)
			{
				if (pid != 0)
				{
					UnixRuntime prev = tasks[pid];
					if (prev == null || prev == this || prev.pid != pid || prev.parent != parent)
					{
						throw new Exception("should never happen");
					}
					lock (parent.children)
					{
						int i = parent.activeChildren.IndexOf(prev);
						if (i == -1)
						{
							throw new Exception("should never happen");
						}
						parent.activeChildren[i] = this;
					}
				}
				else
				{
					int newpid = -1;
					int nextPID = gs.nextPID;
					for (int i = nextPID;i < tasks.Length;i++)
					{
						if (tasks[i] == null)
						{
							newpid = i;
							break;
						}
					}
					if (newpid == -1)
					{
						for (int i = 1;i < nextPID;i++)
						{
							if (tasks[i] == null)
							{
								newpid = i;
								break;
							}
						}
					}
					if (newpid == -1)
					{
						throw new ProcessTableFullExn();
					}
					pid = newpid;
					gs.nextPID = newpid + 1;
				}
				tasks[pid] = this;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: int _syscall(int syscall, int a, int b, int c, int d, int e, int f) throws ErrnoException, FaultException
		internal virtual int _syscall(int syscall, int a, int b, int c, int d, int e, int f)
		{
			switch (syscall)
			{
				case SYS_kill:
					return sys_kill(a,b);
				case SYS_fork:
					return sys_fork();
				case SYS_pipe:
					return sys_pipe(a);
				case SYS_dup2:
					return sys_dup2(a,b);
				case SYS_dup:
					return sys_dup(a);
				case SYS_waitpid:
					return sys_waitpid(a,b,c);
				case SYS_stat:
					return sys_stat(a,b);
				case SYS_lstat:
					return sys_lstat(a,b);
				case SYS_mkdir:
					return sys_mkdir(a,b);
				case SYS_getcwd:
					return sys_getcwd(a,b);
				case SYS_chdir:
					return sys_chdir(a);
				case SYS_exec:
					return sys_exec(a,b,c);
				case SYS_getdents:
					return sys_getdents(a,b,c,d);
				case SYS_unlink:
					return sys_unlink(a);
				case SYS_getppid:
					return sys_getppid();
				case SYS_socket:
					return sys_socket(a,b,c);
				case SYS_connect:
					return sys_connect(a,b,c);
				case SYS_resolve_hostname:
					return sys_resolve_hostname(a,b,c);
				case SYS_setsockopt:
					return sys_setsockopt(a,b,c,d,e);
				case SYS_getsockopt:
					return sys_getsockopt(a,b,c,d,e);
				case SYS_bind:
					return sys_bind(a,b,c);
				case SYS_listen:
					return sys_listen(a,b);
				case SYS_accept:
					return sys_accept(a,b,c);
				case SYS_shutdown:
					return sys_shutdown(a,b);
				case SYS_sysctl:
					return sys_sysctl(a,b,c,d,e,f);
				case SYS_sendto:
					return sys_sendto(a,b,c,d,e,f);
				case SYS_recvfrom:
					return sys_recvfrom(a,b,c,d,e,f);
				case SYS_select:
					return sys_select(a,b,c,d,e);
				case SYS_access:
					return sys_access(a,b);
				case SYS_realpath:
					return sys_realpath(a,b);
				case SYS_chown:
					return sys_chown(a,b,c);
				case SYS_lchown:
					return sys_chown(a,b,c);
				case SYS_fchown:
					return sys_fchown(a,b,c);
				case SYS_chmod:
					return sys_chmod(a,b,c);
				case SYS_fchmod:
					return sys_fchmod(a,b,c);
				case SYS_fcntl:
					return sys_fcntl_lock(a,b,c);
				case SYS_umask:
					return sys_umask(a);

				default:
					return base._syscall(syscall,a,b,c,d,e,f);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: FD _open(String path, int flags, int mode) throws ErrnoException
		internal virtual FD _open(string path, int flags, int mode)
		{
			path = normalizePath(path);
			FD fd = gs.open(this,path,flags,mode);
			if (fd != null && path != null)
			{
				fd.NormalizedPath = path;
			}
			return fd;
		}

		private int sys_getppid()
		{
			return parent == null ? 1 : parent.pid;
		}

		private int sys_chown(int fileAddr, int uid, int gid)
		{
			return 0;
		}
		private int sys_lchown(int fileAddr, int uid, int gid)
		{
			return 0;
		}
		private int sys_fchown(int fd, int uid, int gid)
		{
			return 0;
		}
		private int sys_chmod(int fileAddr, int uid, int gid)
		{
			return 0;
		}
		private int sys_fchmod(int fd, int uid, int gid)
		{
			return 0;
		}
		private int sys_umask(int mask)
		{
			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_access(int cstring, int mode) throws ErrnoException, ReadFaultException
		private int sys_access(int cstringArg, int mode)
		{
			// FEATURE: sys_access
			return gs.stat(this,normalizePath(cstring(cstringArg))) == null ? - ENOENT : 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_realpath(int inAddr, int outAddr) throws FaultException
		private int sys_realpath(int inAddr, int outAddr)
		{
			string s = normalizePath(cstring(inAddr));
			sbyte[] b = getNullTerminatedBytes(s);
			if (b.Length > PATH_MAX)
			{
				return -ERANGE;
			}
			copyout(b,outAddr,b.Length);
			return 0;
		}

		// FEATURE: Signal handling
		// check flag only on backwards jumps to basic blocks without compulsatory checks 
		// (see A Portable Research Framework for the Execution of Java Bytecode - Etienne Gagnon, Chapter 2)

		/// <summary>
		/// The kill syscall.
		///   SIGSTOP, SIGTSTO, SIGTTIN, and SIGTTOUT pause the process.
		///   SIGCONT, SIGCHLD, SIGIO, and SIGWINCH are ignored.
		///   Anything else terminates the process. 
		/// </summary>
		private int sys_kill(int pid, int signal)
		{
			// This will only be called by raise() in newlib to invoke the default handler
			// We don't have to worry about actually delivering the signal
			if (pid != pid)
			{
				return -ESRCH;
			}
			if (signal < 0 || signal >= 32)
			{
				return -EINVAL;
			}
			switch (signal)
			{
				case 0:
					return 0;
				case 17: // SIGSTOP
				case 18: // SIGTSTP
				case 21: // SIGTTIN
				case 22: // SIGTTOU
				case 19: // SIGCONT
				case 20: // SIGCHLD
				case 23: // SIGIO
				case 28: // SIGWINCH
					break;
				default:
					exit(128 + signal, true);
				break;
			}
			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_waitpid(int pid, int statusAddr, int options) throws FaultException, ErrnoException
		private int sys_waitpid(int pid, int statusAddr, int options)
		{
			const int WNOHANG = 1;
			if ((options & ~(WNOHANG)) != 0)
			{
				return -EINVAL;
			}
			if (pid == 0 || pid < -1)
			{
				if (STDERR_DIAG)
				{
					Console.Error.WriteLine("WARNING: waitpid called with a pid of " + pid);
				}
				return -ECHILD;
			}
			bool blocking = (options & WNOHANG) == 0;

			if (pid != -1 && (pid <= 0 || pid >= gs.tasks.Length))
			{
				return -ECHILD;
			}
			if (children == null)
			{
				return blocking ? - ECHILD : 0;
			}

			UnixRuntime done = null;

			lock (children)
			{
				for (;;)
				{
					if (pid == -1)
					{
						if (exitedChildren.Count > 0)
						{
							done = (UnixRuntime)exitedChildren[exitedChildren.Count - 1];
							exitedChildren.RemoveAt(exitedChildren.Count - 1);
						}
					}
					else if (pid > 0)
					{
						if (pid >= gs.tasks.Length)
						{
							return -ECHILD;
						}
						UnixRuntime t = gs.tasks[pid];
						if (t.parent != this)
						{
							return -ECHILD;
						}
						if (t.state == EXITED)
						{
              throw new NotImplementedException();
//							if (!exitedChildren.Remove(t))
//							{
//								throw new Exception("should never happen");
//							}
							done = t;
						}
					}
					else
					{
						// process group stuff, EINVAL returned above
							throw new Exception("should never happen");
					}
					if (done == null)
					{
						if (!blocking)
						{
							return 0;
						}
						try
						{
							Monitor.Wait(children);
						}
						catch (ThreadInterruptedException e)
						{
						}
						//System.err.println("waitpid woke up: " + exitedChildren.size());
					}
					else
					{
						gs.tasks[done.pid] = null;
						break;
					}
				}
			}
			if (statusAddr != 0)
			{
				memWrite(statusAddr,done.exitStatus() << 8);
			}
			return done.pid;
		}


		internal virtual void _exited()
		{
			if (children != null)
			{
				lock (children)
				{
          /*
				for (System.Collections.IEnumerator e = exitedChildren.elements(); e.hasMoreElements();)
				{
					UnixRuntime child = (UnixRuntime) e.nextElement();
					gs.tasks[child.pid] = null;
				}
				exitedChildren.Clear();
				for (System.Collections.IEnumerator e = activeChildren.elements(); e.hasMoreElements();)
				{
					UnixRuntime child = (UnixRuntime) e.nextElement();
					child.parent = null;
				}
				activeChildren.Clear();
    */
          throw new NotImplementedException();
				}
			}

			UnixRuntime _parent = parent;
			if (_parent == null)
			{
				gs.tasks[pid] = null;
			}
			else
			{
				lock (_parent.children)
				{
					if (parent == null)
					{
						gs.tasks[pid] = null;
					}
					else
					{
            /*
						if (!parent.activeChildren.Remove(this))
						{
							throw new Exception("should never happen _exited: pid: " + pid);
						}
      */ throw new NotImplementedException();      
						parent.exitedChildren.Add(this);
						Monitor.Pulse(parent.children);
					}
				}
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected Object clone() throws CloneNotSupportedException
		protected internal virtual object clone()
		{
			UnixRuntime r = (UnixRuntime) base.clone();
			r.pid = 0;
			r.parent = null;
			r.children = null;
			r.activeChildren = r.exitedChildren = null;
			return r;
		}

		private int sys_fork()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final UnixRuntime r;
			UnixRuntime r;

			try
			{
				r = (UnixRuntime) clone();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				return -ENOMEM;
			}

			r.parent = this;

			try
			{
				r._started();
			}
			catch (ProcessTableFullExn e)
			{
				return -ENOMEM;
			}

			//System.err.println("fork " + pid + " -> " + r.pid + " tasks[" + r.pid + "] = " + gd.tasks[r.pid]);
			if (children == null)
			{
				children = new object();
				activeChildren = new ArrayList();
				exitedChildren = new ArrayList();
			}
			activeChildren.Add(r);

			CpuState state = new CpuState();
			getCPUState(state);
			state.r[V0] = 0; // return 0 to child
			state.pc += 4; // skip over syscall instruction
			r.CPUState = state;
			r.state = PAUSED;

			new ForkedProcess(r);

			return r.pid;
		}

		public sealed class ForkedProcess //: System.Threading.Thread
		{
			internal readonly UnixRuntime initial;
			public ForkedProcess(UnixRuntime initial)
			{
				this.initial = initial;
				//start();
        throw new NotImplementedException();
			}
			public void run()
			{
				UnixRuntime.executeAndExec(initial);
			}
		}

		public static int runAndExec(UnixRuntime r, string argv0, string[] rest)
		{
			return runAndExec(r,concatArgv(argv0,rest));
		}
		public static int runAndExec(UnixRuntime r, string[] argv)
		{
			r.start(argv);
			return executeAndExec(r);
		}

		public static int executeAndExec(UnixRuntime r)
		{
			for (;;)
			{
				for (;;)
				{
					if (r.execute())
					{
						break;
					}
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("WARNING: Pause requested while executing runAndExec()");
					}
				}
				if (r.state != EXECED)
				{
					return r.exitStatus();
				}
				r = r.execedRuntime;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private String[] readStringArray(int addr) throws ReadFaultException
		private string[] readStringArray(int addr)
		{
			int count = 0;
			for (int p = addr;memRead(p) != 0;p += 4)
			{
				count++;
			}
			string[] a = new string[count];
			for (int i = 0,p = addr;i < count;i++,p += 4)
			{
				a[i] = cstring(memRead(p));
			}
			return a;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_exec(int cpath, int cargv, int cenvp) throws ErrnoException, FaultException
		private int sys_exec(int cpath, int cargv, int cenvp)
		{
			return exec(normalizePath(cstring(cpath)),readStringArray(cargv),readStringArray(cenvp));
		}

		//private static readonly Method runtimeCompilerCompile;
		static UnixRuntime()
		{
      /*
			Method m;
			try
			{
				m = Type.GetType("org.ibex.nestedvm.RuntimeCompiler").getMethod("compile",new Type[]{typeof(Seekable),typeof(string),typeof(string)});
			}
			catch (NoSuchMethodException e)
			{
				m = null;
			}
			catch (ClassNotFoundException e)
			{
				m = null;
			}
			runtimeCompilerCompile = m;
   */
      throw new NotImplementedException();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Class runtimeCompile(Seekable s, String sourceName) throws IOException
		public virtual Type runtimeCompile(Seekable s, string sourceName)
		{
      throw new NotImplementedException();
      /*
			if (runtimeCompilerCompile == null)
			{
				if (STDERR_DIAG)
				{
					Console.Error.WriteLine("WARNING: Exec attempted but RuntimeCompiler not found!");
				}
				return null;

			}

			try
			{
				return (Type) runtimeCompilerCompile.invoke(null,new object[]{s,"unixruntime,maxinsnpermethod=256,lessconstants",sourceName});
			}
			catch (IllegalAccessException e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				return null;
			}
			catch (InvocationTargetException e)
			{
				Exception t = e.TargetException;
				if (t is IOException)
				{
					throw (IOException) t;
				}
				if (t is Exception)
				{
					throw (Exception) t;
				}
				if (t is Exception)
				{
					throw (Exception) t;
				}
				if (STDERR_DIAG)
				{
					Console.WriteLine(t.ToString());
					Console.Write(t.StackTrace);
				}
				return null;
			}
    */    
    }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int exec(String path, String[] argv, String[] envp) throws ErrnoException
		private int exec(string path, string[] argv, string[] envp)
		{
			if (argv.Length == 0)
			{
				argv = new string[]{""};
			}
			// HACK: Hideous hack to make a standalone busybox possible
			if (path.Equals("bin/busybox") && this.GetType().Name.EndsWith("BusyBox"))
			{
				return execClass(this.GetType(),argv,envp);
			}

			// NOTE: For this little hack to work nestedvm.root MUST be "."
			/*try {
			    System.err.println("Execing normalized path: " + normalizedPath);
			    if(true) return exec(new Interpreter(normalizedPath),argv,envp);
			} catch(IOException e) { throw new Error(e); }*/

			FStat fstat = gs.stat(this,path);
			if (fstat == null)
			{
				return -ENOENT;
			}
			GlobalState.CacheEnt ent = (GlobalState.CacheEnt) gs.execCache[path];
			long mtime = fstat.mtime();
			long size = fstat.size();
			if (ent != null)
			{
				//System.err.println("Found cached entry for " + path);
				if (ent.time == mtime && ent.size == size)
				{
					if (ent.o is Type)
					{
						return execClass((Type) ent.o,argv,envp);
					}
					if (ent.o is string[])
					{
						return execScript(path,(string[]) ent.o,argv,envp);
					}
					throw new Exception("should never happen");
				}
				//System.err.println("Cache was out of date");
				gs.execCache.Remove(path);
			}

			FD fd = gs.open(this,path,RD_ONLY,0);
			if (fd == null)
			{
				throw new ErrnoException(ENOENT);
			}
			Seekable s = fd.seekable();
			if (s == null)
			{
				throw new ErrnoException(EACCES);
			}

			sbyte[] buf = new sbyte[4096];

			try
			{
				int n = s.read(buf,0,buf.Length);
				if (n == -1)
				{
					throw new ErrnoException(ENOEXEC);
				}

				switch (buf[0])
				{
					case 0x7f: //'\177': // possible ELF
						if (n < 4)
						{
							s.tryReadFully(buf,n,4 - n);
						}
						if (buf[1] != 'E' || buf[2] != 'L' || buf[3] != 'F')
						{
							return -ENOEXEC;
						}
						s.seek(0);
						if (STDERR_DIAG)
						{
							Console.Error.WriteLine("Running RuntimeCompiler for " + path);
						}
						Type c = runtimeCompile(s,path);
						if (STDERR_DIAG)
						{
							Console.Error.WriteLine("RuntimeCompiler finished for " + path);
						}
						if (c == null)
						{
							throw new ErrnoException(ENOEXEC);
						}
						gs.execCache[path] = new GlobalState.CacheEnt(mtime,size,c);
						return execClass(c,argv,envp);
					case (sbyte)'#':
						if (n == 1)
						{
							int n2 = s.read(buf,1,buf.Length - 1);
							if (n2 == -1)
							{
								return -ENOEXEC;
							}
							n += n2;
						}
						if (buf[1] != '!')
						{
							return -ENOEXEC;
						}
						int p = 2;
						n -= 2;
						for (;;)
						{
							for (int i = p;i < p + n;i++)
							{
								if (buf[i] == '\n')
								{
									p = i;
									goto OUTERBreak;
								}
							}
								p += n;
							if (p == buf.Length)
							{
								goto OUTERBreak;
							}
							n = s.read(buf,p,buf.Length - p);
							OUTERContinue:;
						}
						OUTERBreak:
						int cmdStart = 2;
						for (;cmdStart < p;cmdStart++)
						{
							if (buf[cmdStart] != ' ')
							{
								break;
							}
						}
						if (cmdStart == p)
						{
							throw new ErrnoException(ENOEXEC);
						}
						int argStart = cmdStart;
						for (;argStart < p;argStart++)
						{
							if (buf[argStart] == ' ')
							{
								break;
							}
						}
						int cmdEnd = argStart;
						while (argStart < p && buf[argStart] == ' ')
						{
							argStart++;
						}
            throw new NotImplementedException();
            string[] command = null;
//						string[] command = new string[] {
//              new string(buf,cmdStart,cmdEnd - cmdStart), 
//              argStart < p ? new string(buf,argStart,p - argStart) : null
//            };
						gs.execCache[path] = new GlobalState.CacheEnt(mtime,size,command);
						return execScript(path,command,argv,envp);
					default:
						return -ENOEXEC;
				}
			}
			catch (IOException e)
			{
				return -EIO;
			}
			finally
			{
				fd.close();
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int execScript(String path, String[] command, String[] argv, String[] envp) throws ErrnoException
		public virtual int execScript(string path, string[] command, string[] argv, string[] envp)
		{
			string[] newArgv = new string[argv.Length - 1 + (command[1] != null ? 3 : 2)];
			int p = command[0].LastIndexOf('/');
			newArgv[0] = p == -1 ? command[0] : command[0].Substring(p + 1);
			newArgv[1] = "/" + path;
			p = 2;
			if (command[1] != null)
			{
				newArgv[p++] = command[1];
			}
			for (int i = 1;i < argv.Length;i++)
			{
				newArgv[p++] = argv[i];
			}
			if (p != newArgv.Length)
			{
				throw new Exception("p != newArgv.length");
			}
			Console.Error.WriteLine("Execing: " + command[0]);
			for (int i = 0;i < newArgv.Length;i++)
			{
				Console.Error.WriteLine("execing [" + i + "] " + newArgv[i]);
			}
			return exec(command[0],newArgv,envp);
		}

		public virtual int execClass(Type c, string[] argv, string[] envp)
		{
			try
			{
        throw new NotImplementedException();
//				UnixRuntime r = (UnixRuntime) c.getDeclaredConstructor(new Type[]{bool.TYPE}).newInstance(new object[]{true});
//				return exec(r,argv,envp);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				return -ENOEXEC;
			}
		}

		private int exec(UnixRuntime r, string[] argv, string[] envp)
		{
			//System.err.println("Execing " + r);
			for (int i = 0;i < OPEN_MAX;i++)
			{
				if (closeOnExec[i])
				{
					closeFD(i);
				}
			}
			r.fds = fds;
			r.closeOnExec = closeOnExec;
			// make sure this doesn't get messed with these since we didn't copy them
			fds = null;
			closeOnExec = null;

			r.gs = gs;
			r.sm = sm;
			r.cwd = cwd;
			r.pid = pid;
			r.parent = parent;
			r.start(argv,envp);

			state = EXECED;
			execedRuntime = r;

			return 0;
		}

		public class Pipe
		{
			internal bool InstanceFieldsInitialized = false;

			public Pipe()
			{
				if (!InstanceFieldsInitialized)
				{
					InitializeInstanceFields();
					InstanceFieldsInitialized = true;
				}
			}

			internal virtual void InitializeInstanceFields()
			{
				reader = new Reader(this);
				writer = new Writer(this);
			}

			internal readonly sbyte[] pipebuf = new sbyte[PIPE_BUF * 4];
			internal int readPos;
			internal int writePos;

			public FD reader;
			public FD writer;

			public class Reader : FD
			{
				private readonly UnixRuntime.Pipe outerInstance;

				public Reader(UnixRuntime.Pipe outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				protected internal override FStat _fstat()
				{
					return new SocketFStat();
				}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int read(byte[] buf, int off, int len) throws ErrnoException
				public virtual int read(sbyte[] buf, int off, int len)
				{
					if (len == 0)
					{
						return 0;
					}
					lock (outerInstance)
					{
						while (outerInstance.writePos != -1 && outerInstance.readPos == outerInstance.writePos)
						{
							try // ignore
							{
								Monitor.Wait(outerInstance);
							}
							catch (ThreadInterruptedException e)
							{
							}
						}
						if (outerInstance.writePos == -1) // eof
						{
							return 0;
						}
						len = Math.Min(len,outerInstance.writePos - outerInstance.readPos);
						Array.Copy(outerInstance.pipebuf,outerInstance.readPos,buf,off,len);
						outerInstance.readPos += len;
						if (outerInstance.readPos == outerInstance.writePos)
						{
							Monitor.Pulse(outerInstance);
						}
						return len;
					}
				}
				public override int flags()
				{
					return O_RDONLY;
				}
				public virtual void _close()
				{
					lock (outerInstance)
					{
						outerInstance.readPos = -1;
						Monitor.Pulse(outerInstance);
					}
				}
			}

			public class Writer : FD
			{
				private readonly UnixRuntime.Pipe outerInstance;

				public Writer(UnixRuntime.Pipe outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				protected internal override FStat _fstat()
				{
					return new SocketFStat();
				}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int write(byte[] buf, int off, int len) throws ErrnoException
				public virtual int write(sbyte[] buf, int off, int len)
				{
					if (len == 0)
					{
						return 0;
					}
					lock (outerInstance)
					{
						if (outerInstance.readPos == -1)
						{
							throw new ErrnoException(EPIPE);
						}
						if (outerInstance.pipebuf.Length - outerInstance.writePos < Math.Min(len,PIPE_BUF))
						{
							// not enough space to atomicly write the data
							while (outerInstance.readPos != -1 && outerInstance.readPos != outerInstance.writePos)
							{
								try // ignore
								{
									Monitor.Wait(outerInstance);
								}
								catch (ThreadInterruptedException e)
								{
								}
							}
							if (outerInstance.readPos == -1)
							{
								throw new ErrnoException(EPIPE);
							}
							outerInstance.readPos = outerInstance.writePos = 0;
						}
						len = Math.Min(len,outerInstance.pipebuf.Length - outerInstance.writePos);
						Array.Copy(buf,off,outerInstance.pipebuf,outerInstance.writePos,len);
						if (outerInstance.readPos == outerInstance.writePos)
						{
							Monitor.Pulse(outerInstance);
						}
						outerInstance.writePos += len;
						return len;
					}
				}
				public override int flags()
				{
					return O_WRONLY;
				}
				public virtual void _close()
				{
					lock (outerInstance)
					{
						outerInstance.writePos = -1;
						Monitor.Pulse(outerInstance);
					}
				}
			}
		}

		private int sys_pipe(int addr)
		{
			Pipe pipe = new Pipe();

			int fd1 = addFD(pipe.reader);
			if (fd1 < 0)
			{
				return -ENFILE;
			}
			int fd2 = addFD(pipe.writer);
			if (fd2 < 0)
			{
				closeFD(fd1);
				return -ENFILE;
			}

			try
			{
				memWrite(addr,fd1);
				memWrite(addr + 4,fd2);
			}
			catch (FaultException e)
			{
				closeFD(fd1);
				closeFD(fd2);
				return -EFAULT;
			}
			return 0;
		}

		private int sys_dup2(int oldd, int newd)
		{
			if (oldd == newd)
			{
				return 0;
			}
			if (oldd < 0 || oldd >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (newd < 0 || newd >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[oldd] == null)
			{
				return -EBADFD;
			}
			if (fds[newd] != null)
			{
				fds[newd].close();
			}
			fds[newd] = fds[oldd].dup();
			return 0;
		}

		private int sys_dup(int oldd)
		{
			if (oldd < 0 || oldd >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[oldd] == null)
			{
				return -EBADFD;
			}
			FD fd = fds[oldd].dup();
			int newd = addFD(fd);
			if (newd < 0)
			{
				fd.close();
				return -ENFILE;
			}
			return newd;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_stat(int cstring, int addr) throws FaultException, ErrnoException
		private int sys_stat(int cstringArg, int addr)
		{
			FStat s = gs.stat(this,normalizePath(cstring(cstringArg)));
			if (s == null)
			{
				return -ENOENT;
			}
			return stat(s,addr);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_lstat(int cstring, int addr) throws FaultException, ErrnoException
		private int sys_lstat(int cstringArg, int addr)
		{
			FStat s = gs.lstat(this,normalizePath(cstring(cstringArg)));
			if (s == null)
			{
				return -ENOENT;
			}
			return stat(s,addr);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_mkdir(int cstring, int mode) throws FaultException, ErrnoException
    private int sys_mkdir(int cstringArg, int mode)
		{
      gs.mkdir(this,normalizePath(cstring(cstringArg)),mode);
			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_unlink(int cstring) throws FaultException, ErrnoException
    private int sys_unlink(int cstringArg)
		{
      gs.unlink(this,normalizePath(cstring(cstringArg)));
			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_getcwd(int addr, int size) throws FaultException, ErrnoException
		private int sys_getcwd(int addr, int size)
		{
			sbyte[] b = getBytes(cwd);
			if (size == 0)
			{
				return -EINVAL;
			}
			if (size < b.Length + 2)
			{
				return -ERANGE;
			}
			memset(addr,'/',1);
			copyout(b,addr + 1,b.Length);
			memset(addr + b.Length + 1,0,1);
			return addr;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_chdir(int addr) throws ErrnoException, FaultException
		private int sys_chdir(int addr)
		{
			string path = normalizePath(cstring(addr));
			FStat st = gs.stat(this,path);
			if (st == null)
			{
				return -ENOENT;
			}
			if (st.type() != FStat.S_IFDIR)
			{
				return -ENOTDIR;
			}
			cwd = path;
			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_getdents(int fdn, int addr, int count, int seekptr) throws FaultException, ErrnoException
		private int sys_getdents(int fdn, int addr, int count, int seekptr)
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
			int n = fds[fdn].getdents(buf,0,count);
			copyout(buf,addr,n);
			return n;
		}

		internal virtual void _preCloseFD(FD fd)
		{
			// release all fcntl locks on this file
			Seekable s = fd.seekable();
			if (s == null)
			{
				return;
			}

			try
			{
				for (int i = 0; i < gs.locks.Length; i++)
				{
					Lock l = gs.locks[i];
					if (l == null)
					{
						continue;
					}
					if (s.Equals(l.seekable()) && l.Owner == this)
					{
						l.release();
						gs.locks[i] = null;
					}
				}
			}
			catch (IOException e)
			{
				throw new Exception("oops",e);
			}
		}

		internal virtual void _postCloseFD(FD fd)
		{
			if (fd.MarkedForDeleteOnClose)
			{
				try
				{
					gs.unlink(this, fd.NormalizedPath);
				}
				catch (Exception t)
				{
				}
			}
		}

		/// <summary>
		/// Implements the F_GETLK and F_SETLK cases of fcntl syscall.
		///  If l_start = 0 and l_len = 0 the lock refers to the entire file.
		///  Uses GlobalState to ensure locking across processes in the same JVM.
		/// struct flock {
		///   short   l_type;         // lock type: F_UNLCK, F_RDLCK, F_WRLCK
		///   short   l_whence;       // type of l_start: SEEK_SET, SEEK_CUR, SEEK_END
		///   long    l_start;        // starting offset, bytes
		///   long    l_len;          // len = 0 means until EOF
		///   short   l_pid;          // lock owner
		///   short   l_xxx;          // padding
		/// };
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_fcntl_lock(int fdn, int cmd, int arg) throws FaultException
		private int sys_fcntl_lock(int fdn, int cmd, int arg)
		{
      /*
			if (cmd != F_GETLK && cmd != F_SETLK)
			{
				return sys_fcntl(fdn, cmd, arg);
			}

			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				return -EBADFD;
			}
			if (fds[fdn] == null)
			{
				return -EBADFD;
			}
			FD fd = fds[fdn];

			if (arg == 0)
			{
				return -EINVAL;
			}
			int word = memRead(arg);
			int l_start = memRead(arg + 4);
			int l_len = memRead(arg + 8);
			int l_type = word >> 16;
			int l_whence = word & 0x00ff;

			Seekable.Lock[] locks = gs.locks;
			Seekable s = fd.seekable();
			if (s == null)
			{
				return -EINVAL;
			}

			try
			{

			switch (l_whence)
			{
				case SEEK_SET:
					break;
				case SEEK_CUR:
					l_start += s.pos();
					break;
				case SEEK_END:
					l_start += s.length();
					break;
				default:
					return -1;
			}

			if (cmd == F_GETLK)
			{
				// The simple Java file locking below will happily return
				// a lock that overlaps one already held by the JVM. Thus
				// we must check over all the locks held by other Runtimes
				for (int i = 0; i < locks.Length; i++)
				{
					if (locks[i] == null || !s.Equals(locks[i].seekable()))
					{
						continue;
					}
					if (!locks[i].overlaps(l_start, l_len))
					{
						continue;
					}
					if (locks[i].Owner == this)
					{
						continue;
					}
					if (locks[i].Shared && l_type == F_RDLCK)
					{
						continue;
					}

					// overlapping lock held by another process
					return 0;
				}

				// check if an area is lockable by attempting to obtain a lock
				Seekable.Lock @lock = s.@lock(l_start, l_len, l_type == F_RDLCK);

				if (@lock != null) // no lock exists
				{
					memWrite(arg, SEEK_SET | (F_UNLCK << 16));
					@lock.release();
				}

				return 0;
			}

			// now processing F_SETLK
			if (cmd != F_SETLK)
			{
				return -EINVAL;
			}

			if (l_type == F_UNLCK)
			{
				// release all locks that fall within the boundaries given
				for (int i = 0; i < locks.Length; i++)
				{
					if (locks[i] == null || !s.Equals(locks[i].seekable()))
					{
						continue;
					}
					if (locks[i].Owner != this)
					{
						continue;
					}

					int pos = (int)locks[i].position();
					if (pos < l_start)
					{
						continue;
					}
					if (l_start != 0 && l_len != 0) // start/len 0 means unlock all
					{
						if (pos + locks[i].size() > l_start + l_len)
						{
							continue;
						}
					}

					locks[i].release();
					locks[i] = null;
				}
				return 0;

			}
			else if (l_type == F_RDLCK || l_type == F_WRLCK)
			{
				// first see if a lock already exists
				for (int j = 0; j < locks.Length; j++)
				{
					if (locks[j] == null || !s.Equals(locks[j].seekable()))
					{
						continue;
					}

					if (locks[j].Owner == this)
					{
						// if this Runtime owns an overlapping lock work with it
						if (locks[j].contained(l_start, l_len))
						{
							locks[j].release();
							locks[j] = null;
						}
						else if (locks[j].contains(l_start, l_len))
						{
							if (locks[j].Shared == (l_type == F_RDLCK))
							{
								// return this more general lock
								memWrite(arg + 4, (int)locks[j].position());
								memWrite(arg + 8, (int)locks[j].size());
								return 0;
							}
							else
							{
								locks[j].release();
								locks[j] = null;
							}
						}
					}
					else
					{
						// if another Runtime has an lock and it is exclusive or
						// we want an exclusive lock then fail
						if (locks[j].overlaps(l_start, l_len) && (!locks[j].Shared || l_type == F_WRLCK))
						{
							return -EAGAIN;
						}
					}
				}

				// create the lock
				Seekable.Lock @lock = s.@lock(l_start, l_len, l_type == F_RDLCK);
				if (@lock == null)
				{
					return -EAGAIN;
				}
				@lock.Owner = this;

				int i;
				for (i = 0; i < locks.Length; i++)
				{
					if (locks[i] == null)
					{
						break;
					}
				}
				if (i == locks.Length)
				{
					return -ENOLCK;
				}
				locks[i] = @lock;
				return 0;

			}
			else
			{
				return -EINVAL;
			}

			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
   */
      throw new NotImplementedException();
		}

		internal class SocketFD : FD
		{
			public const int TYPE_STREAM = 0;
			public const int TYPE_DGRAM = 1;
			public const int LISTEN = 2;
			public virtual int type()
			{
				return flags_Renamed & 1;
			}
			public virtual bool listen()
			{
				return (flags_Renamed & 2) != 0;
			}

			internal int flags_Renamed;
			internal int options;

			internal Socket s;
			internal Socket ss;
      internal Socket ds;

			internal IPAddress bindAddr;
			internal int bindPort = -1;
			internal IPAddress connectAddr;
			internal int connectPort = -1;

			internal byte[] dp;
			internal InputStream @is;
			internal OutputStream os;

			internal static readonly sbyte[] EMPTY = new sbyte[0];
			public SocketFD(int type)
			{
				flags_Renamed = type;
				if (type == TYPE_DGRAM)
				{
          dp = new byte[0];//new DatagramPacket(EMPTY,0);
				}
			}

			public virtual void setOptions()
			{
				try
				{
					if (s != null && type() == TYPE_STREAM && !listen())
					{
						Platform.socketSetKeepAlive(s,(options & SO_KEEPALIVE) != 0);
					}
				}
				catch (SocketException e)
				{
					if (STDERR_DIAG)
					{
						Console.WriteLine(e.ToString());
						Console.Write(e.StackTrace);
					}
				}
			}

			public virtual void _close()
			{
				try
				{
				   if (s != null)
				   {
					   s.Close();
				   }
				   if (ss != null)
				   {
            ss.Close();
				   }
				   if (ds != null)
				   {
            ds.Close();
				   }
				}
				catch (IOException e)
				{
					/* ignore */
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int read(byte[] a, int off, int length) throws ErrnoException
			public virtual int read(sbyte[] a, int off, int length)
			{
				if (type() == TYPE_DGRAM)
				{
					return recvfrom(a,off,length,null,null);
				}
				if (@is == null)
				{
					throw new ErrnoException(EPIPE);
				}
				try
				{
					int n = @is.read(a,off,length);
					return n < 0 ? 0 : n;
				}
				catch (IOException e)
				{
					throw new ErrnoException(EIO);
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int recvfrom(byte[] a, int off, int length, InetAddress[] sockAddr, int[] port) throws ErrnoException
			public virtual int recvfrom(sbyte[] a, int off, int length, IPAddress[] sockAddr, int[] port)
			{
        /*
				if (type() == TYPE_STREAM)
				{
					return read(a,off,length);
				}

				if (off != 0)
				{
					throw new System.ArgumentException("off must be 0");
				}
				dp.Data = a;
				dp.Length = length;
				try
				{
					if (ds == null)
					{
						ds = new DatagramSocket();
					}
					ds.receive(dp);
				}
				catch (IOException e)
				{
					if (STDERR_DIAG)
					{
						Console.WriteLine(e.ToString());
						Console.Write(e.StackTrace);
					}
					throw new ErrnoException(EIO);
				}
				if (sockAddr != null)
				{
					sockAddr[0] = dp.Address;
					port[0] = dp.Port;
				}
				return dp.Length;
    */ throw new NotImplementedException();    
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int write(byte[] a, int off, int length) throws ErrnoException
			public virtual int write(sbyte[] a, int off, int length)
			{
				if (type() == TYPE_DGRAM)
				{
					return sendto(a,off,length,null,-1);
				}

				if (os == null)
				{
					throw new ErrnoException(EPIPE);
				}
				try
				{
					os.write(a,off,length);
					return length;
				}
				catch (IOException e)
				{
					throw new ErrnoException(EIO);
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int sendto(byte[] a, int off, int length, InetAddress destAddr, int destPort) throws ErrnoException
			public virtual int sendto(sbyte[] a, int off, int length, IPAddress destAddr, int destPort)
			{

				if (off != 0)
				{
					throw new System.ArgumentException("off must be 0");
				}
				if (type() == TYPE_STREAM)
				{
					return write(a,off,length);
				}

				if (destAddr == null)
				{
					destAddr = connectAddr;
					destPort = connectPort;

					if (destAddr == null)
					{
						throw new ErrnoException(ENOTCONN);
					}
				}
        throw new NotImplementedException();
        /*
				dp.Address = destAddr;
				dp.Port = destPort;
				dp.Data = a;
				dp.Length = length;

				try
				{
					if (ds == null)
					{
						ds = new DatagramSocket();
					}
					ds.send(dp);
				}
				catch (IOException e)
				{
					if (STDERR_DIAG)
					{
						Console.WriteLine(e.ToString());
						Console.Write(e.StackTrace);
					}
					if ("Network is unreachable".Equals(e.Message))
					{
						throw new ErrnoException(EHOSTUNREACH);
					}
					throw new ErrnoException(EIO);
				}
    */    
				return dp.Length;
			}

			public override int flags()
			{
				return O_RDWR;
			}
			protected internal override FStat _fstat()
			{
				return new SocketFStat();
			}
		}

		private int sys_socket(int domain, int type, int proto)
		{
			if (domain != AF_INET || (type != SOCK_STREAM && type != SOCK_DGRAM))
			{
				return -EPROTONOSUPPORT;
			}
			return addFD(new SocketFD(type == SOCK_STREAM ? SocketFD.TYPE_STREAM : SocketFD.TYPE_DGRAM));
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private SocketFD getSocketFD(int fdn) throws ErrnoException
		private SocketFD getSocketFD(int fdn)
		{
			if (fdn < 0 || fdn >= OPEN_MAX)
			{
				throw new ErrnoException(EBADFD);
			}
			if (fds[fdn] == null)
			{
				throw new ErrnoException(EBADFD);
			}
			if (!(fds[fdn] is SocketFD))
			{
				throw new ErrnoException(ENOTSOCK);
			}

			return (SocketFD) fds[fdn];
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_connect(int fdn, int addr, int namelen) throws ErrnoException, FaultException
		private int sys_connect(int fdn, int addr, int namelen)
		{
			SocketFD fd = getSocketFD(fdn);

			if (fd.type() == SocketFD.TYPE_STREAM && (fd.s != null || fd.ss != null))
			{
				return -EISCONN;
			}
			int word1 = memRead(addr);
			if ((((int)((uint)word1 >> 16)) & 0xff) != AF_INET)
			{
				return -EAFNOSUPPORT;
			}
			int port = word1 & 0xffff;
			sbyte[] ip = new sbyte[4];
			copyin(addr + 4,ip,4);

			IPAddress inetAddr;
			try
			{
				inetAddr = Platform.inetAddressFromBytes(ip);
			}
			catch (Exception e)
			{
				return -EADDRNOTAVAIL;
			}

			fd.connectAddr = inetAddr;
			fd.connectPort = port;

			try
			{
				switch (fd.type())
				{
					case SocketFD.TYPE_STREAM:
					{
            throw new NotImplementedException(); 

						//Socket s = new Socket(inetAddr,port);
						//fd.s = s;
						fd.setOptions();
//						fd.@is = s.InputStream;
//						fd.os = s.OutputStream;
						break;
					}
					case SocketFD.TYPE_DGRAM:
						break;
					default:
						throw new Exception("should never happen");
				}
			}
			catch (IOException e)
			{
				return -ECONNREFUSED;
			}

			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_resolve_hostname(int chostname, int addr, int sizeAddr) throws FaultException
		private int sys_resolve_hostname(int chostname, int addr, int sizeAddr)
		{
      /*
			string hostname = cstring(chostname);
			int size = memRead(sizeAddr);
			InetAddress[] inetAddrs;
			try
			{
				inetAddrs = InetAddress.getAllByName(hostname);
			}
			catch (UnknownHostException e)
			{
				return HOST_NOT_FOUND;
			}
			int count = min(size / 4,inetAddrs.Length);
			for (int i = 0;i < count;i++,addr += 4)
			{
				sbyte[] b = inetAddrs[i].Address;
				copyout(b,addr,4);
			}
			memWrite(sizeAddr,count * 4);
			return 0;
   */ throw new NotImplementedException();   
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_setsockopt(int fdn, int level, int name, int valaddr, int len) throws ReadFaultException, ErrnoException
		private int sys_setsockopt(int fdn, int level, int name, int valaddr, int len)
		{
			SocketFD fd = getSocketFD(fdn);
			switch (level)
			{
				case SOL_SOCKET:
					switch (name)
					{
						case SO_REUSEADDR:
						case SO_KEEPALIVE:
						{
							if (len != 4)
							{
								return -EINVAL;
							}
							int val = memRead(valaddr);
							if (val != 0)
							{
								fd.options |= name;
							}
							else
							{
								fd.options &= ~name;
							}
							fd.setOptions();
							return 0;
						}
						default:
							if (STDERR_DIAG)
							{
								Console.Error.WriteLine("Unknown setsockopt name passed: " + name);
							}
							return -ENOPROTOOPT;
					}
				default:
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("Unknown setsockopt leve passed: " + level);
					}
					return -ENOPROTOOPT;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_getsockopt(int fdn, int level, int name, int valaddr, int lenaddr) throws ErrnoException, FaultException
		private int sys_getsockopt(int fdn, int level, int name, int valaddr, int lenaddr)
		{
			SocketFD fd = getSocketFD(fdn);
			switch (level)
			{
				case SOL_SOCKET:
					switch (name)
					{
						case SO_REUSEADDR:
						case SO_KEEPALIVE:
						{
							int len = memRead(lenaddr);
							if (len < 4)
							{
								return -EINVAL;
							}
							int val = (fd.options & name) != 0 ? 1 : 0;
							memWrite(valaddr,val);
							memWrite(lenaddr,4);
							return 0;
						}
						default:
							if (STDERR_DIAG)
							{
								Console.Error.WriteLine("Unknown setsockopt name passed: " + name);
							}
							return -ENOPROTOOPT;
					}
				default:
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("Unknown setsockopt leve passed: " + level);
					}
					return -ENOPROTOOPT;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_bind(int fdn, int addr, int namelen) throws FaultException, ErrnoException
		private int sys_bind(int fdn, int addr, int namelen)
		{
			SocketFD fd = getSocketFD(fdn);

			if (fd.type() == SocketFD.TYPE_STREAM && (fd.s != null || fd.ss != null))
			{
				return -EISCONN;
			}
			int word1 = memRead(addr);
			if ((((int)((uint)word1 >> 16)) & 0xff) != AF_INET)
			{
				return -EAFNOSUPPORT;
			}
			int port = word1 & 0xffff;
			IPAddress inetAddr = null;
			if (memRead(addr + 4) != 0)
			{
				sbyte[] ip = new sbyte[4];
				copyin(addr + 4,ip,4);

				try
				{
					inetAddr = Platform.inetAddressFromBytes(ip);
				}
				catch (Exception e)
				{
					return -EADDRNOTAVAIL;
				}
			}

			switch (fd.type())
			{
				case SocketFD.TYPE_STREAM:
				{
					fd.bindAddr = inetAddr;
					fd.bindPort = port;
					return 0;
				}
				case SocketFD.TYPE_DGRAM:
				{
					if (fd.ds != null)
					{
            throw new NotImplementedException();
						//fd.ds.close();
					}
					try
					{
            throw new NotImplementedException();
            //fd.ds = inetAddr != null ? new DatagramSocket(port,inetAddr) : new DatagramSocket(port);
					}
					catch (IOException e)
					{
						return -EADDRINUSE;
					}
					return 0;
				}
				default:
					throw new Exception("should never happen");
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_listen(int fdn, int backlog) throws ErrnoException
		private int sys_listen(int fdn, int backlog)
		{
			SocketFD fd = getSocketFD(fdn);
			if (fd.type() != SocketFD.TYPE_STREAM)
			{
				return -EOPNOTSUPP;
			}
			if (fd.ss != null || fd.s != null)
			{
				return -EISCONN;
			}
			if (fd.bindPort < 0)
			{
				return -EOPNOTSUPP;
			}

			try
			{
        throw new NotImplementedException();
        //fd.ss = new ServerSocket(fd.bindPort,backlog,fd.bindAddr);
				fd.flags_Renamed |= SocketFD.LISTEN;
				return 0;
			}
			catch (IOException e)
			{
				return -EADDRINUSE;
			}

		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_accept(int fdn, int addr, int lenaddr) throws ErrnoException, FaultException
		private int sys_accept(int fdn, int addr, int lenaddr)
		{
      throw new NotImplementedException();
      /*
			SocketFD fd = getSocketFD(fdn);
			if (fd.type() != SocketFD.TYPE_STREAM)
			{
				return -EOPNOTSUPP;
			}
			if (!fd.listen())
			{
				return -EOPNOTSUPP;
			}

			int size = memRead(lenaddr);

      ServerSocket s = fd.ss;
			Socket client;
			try
			{
        client = s.accept();
			}
			catch (IOException e)
			{
				return -EIO;
			}

			if (size >= 8)
			{
				memWrite(addr,(6 << 24) | (AF_INET << 16) | client.Port);
				sbyte[] b = client.InetAddress.Address;
				copyout(b,addr + 4,4);
				memWrite(lenaddr,8);
			}

			SocketFD clientFD = new SocketFD(SocketFD.TYPE_STREAM);
			clientFD.s = client;
			try
			{
				clientFD.@is = client.InputStream;
				clientFD.os = client.OutputStream;
			}
			catch (IOException e)
			{
				return -EIO;
			}
			int n = addFD(clientFD);
			if (n == -1)
			{
				clientFD.close();
				return -ENFILE;
			}
			return n;
   */   
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_shutdown(int fdn, int how) throws ErrnoException
		private int sys_shutdown(int fdn, int how)
		{
			SocketFD fd = getSocketFD(fdn);
			if (fd.type() != SocketFD.TYPE_STREAM || fd.listen())
			{
				return -EOPNOTSUPP;
			}
			if (fd.s == null)
			{
				return -ENOTCONN;
			}

			Socket s = fd.s;

			try
			{
				if (how == SHUT_RD || how == SHUT_RDWR)
				{
					Platform.socketHalfClose(s,false);
				}
				if (how == SHUT_WR || how == SHUT_RDWR)
				{
					Platform.socketHalfClose(s,true);
				}
			}
			catch (IOException e)
			{
				return -EIO;
			}

			return 0;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_sendto(int fdn, int addr, int count, int flags, int destAddr, int socklen) throws ErrnoException,ReadFaultException
		private int sys_sendto(int fdn, int addr, int count, int flags, int destAddr, int socklen)
		{
			SocketFD fd = getSocketFD(fdn);
			if (flags != 0)
			{
				throw new ErrnoException(EINVAL);
			}

			int word1 = memRead(destAddr);
			if ((((int)((uint)word1 >> 16)) & 0xff) != AF_INET)
			{
				return -EAFNOSUPPORT;
			}
			int port = word1 & 0xffff;
			IPAddress inetAddr;
			sbyte[] ip = new sbyte[4];
			copyin(destAddr + 4,ip,4);
			try
			{
				inetAddr = Platform.inetAddressFromBytes(ip);
			}
			catch (Exception e)
			{
				return -EADDRNOTAVAIL;
			}

			count = Math.Min(count,MAX_CHUNK);
			sbyte[] buf = byteBuf(count);
			copyin(addr,buf,count);
			try
			{
				return fd.sendto(buf,0,count,inetAddr,port);
			}
			catch (ErrnoException e)
			{
				if (e.errno == EPIPE)
				{
					exit(128 + 13,true);
				}
				throw e;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_recvfrom(int fdn, int addr, int count, int flags, int sourceAddr, int socklenAddr) throws ErrnoException, FaultException
		private int sys_recvfrom(int fdn, int addr, int count, int flags, int sourceAddr, int socklenAddr)
		{
			SocketFD fd = getSocketFD(fdn);
			if (flags != 0)
			{
				throw new ErrnoException(EINVAL);
			}

			IPAddress[] inetAddr = sourceAddr == 0 ? null : new IPAddress[1];
			int[] port = sourceAddr == 0 ? null : new int[1];

			count = Math.Min(count,MAX_CHUNK);
			sbyte[] buf = byteBuf(count);
			int n = fd.recvfrom(buf,0,count,inetAddr,port);
			copyout(buf,addr,n);

			if (sourceAddr != 0)
			{
				memWrite(sourceAddr,(AF_INET << 16) | port[0]);
        byte[] foo =inetAddr[0].GetAddressBytes();
        sbyte[] ip =  new sbyte[foo.Length]; //inetAddr[0].Address;
        Array.Copy(foo,ip,foo.Length);
				copyout(ip,sourceAddr + 4,4);
			}

			return n;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_select(int n, int readFDs, int writeFDs, int exceptFDs, int timevalAddr) throws ReadFaultException, ErrnoException
		private int sys_select(int n, int readFDs, int writeFDs, int exceptFDs, int timevalAddr)
		{
			return -ENOSYS;
		}

		private static string hostName()
		{
			try
			{
        return "localhost";//TODO: InetAddress.LocalHost.HostName;
			}
			catch (Exception e)
			{
				return "darkstar";
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int sys_sysctl(int nameaddr, int namelen, int oldp, int oldlenaddr, int newp, int newlen) throws FaultException
		private int sys_sysctl(int nameaddr, int namelen, int oldp, int oldlenaddr, int newp, int newlen)
		{
			if (newp != 0)
			{
				return -EPERM;
			}
			if (namelen == 0)
			{
				return -ENOENT;
			}
			if (oldp == 0)
			{
				return 0;
			}

			object o = null;
			switch (memRead(nameaddr))
			{
				case CTL_KERN:
					if (namelen != 2)
					{
						break;
					}
					switch (memRead(nameaddr + 4))
					{
						case KERN_OSTYPE:
							o = "NestedVM";
							break;
						case KERN_HOSTNAME:
							o = hostName();
							break;
						case KERN_OSRELEASE:
							o = VERSION;
							break;
						case KERN_VERSION:
							o = "NestedVM Kernel Version " + VERSION;
							break;
					}
					break;
				case CTL_HW:
					if (namelen != 2)
					{
						break;
					}
					switch (memRead(nameaddr + 4))
					{
						case HW_MACHINE:
							o = "NestedVM Virtual Machine";
							break;
					}
					break;
			}
			if (o == null)
			{
				return -ENOENT;
			}
			int len = memRead(oldlenaddr);
			if (o is string)
			{
				sbyte[] b = getNullTerminatedBytes((string)o);
				if (len < b.Length)
				{
					return -ENOMEM;
				}
				len = b.Length;
				copyout(b,oldp,len);
				memWrite(oldlenaddr,len);
			}
			else if (o is int?)
			{
				if (len < 4)
				{
					return -ENOMEM;
				}
				memWrite(oldp,(int)((int?)o));
			}
			else
			{
				throw new Exception("should never happen");
			}
			return 0;
		}


		public abstract class FS
		{
			internal const int OPEN = 1;
			internal const int STAT = 2;
			internal const int LSTAT = 3;
			internal const int MKDIR = 4;
			internal const int UNLINK = 5;

			internal GlobalState owner;
			internal int devno;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Object dispatch(int op, UnixRuntime r, String path, int arg1, int arg2) throws ErrnoException
			internal virtual object dispatch(int op, UnixRuntime r, string path, int arg1, int arg2)
			{
				switch (op)
				{
					case OPEN:
						return open(r,path,arg1,arg2);
					case STAT:
						return stat(r,path);
					case LSTAT:
						return lstat(r,path);
					case MKDIR:
						mkdir(r,path,arg1);
						return null;
					case UNLINK:
						unlink(r,path);
						return null;
					default:
						throw new Exception("should never happen");
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FStat lstat(UnixRuntime r, String path) throws ErrnoException
			public virtual FStat lstat(UnixRuntime r, string path)
			{
				return stat(r,path);
			}

			// If this returns null it'll be truned into an ENOENT
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract FD open(UnixRuntime r, String path, int flags, int mode) throws ErrnoException;
			public abstract FD open(UnixRuntime r, string path, int flags, int mode);
			// If this returns null it'll be turned into an ENOENT
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract FStat stat(UnixRuntime r, String path) throws ErrnoException;
			public abstract FStat stat(UnixRuntime r, string path);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract void mkdir(UnixRuntime r, String path, int mode) throws ErrnoException;
			public abstract void mkdir(UnixRuntime r, string path, int mode);
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract void unlink(UnixRuntime r, String path) throws ErrnoException;
			public abstract void unlink(UnixRuntime r, string path);
		}

		// chroot support should go in here if it is ever implemented
		private string normalizePath(string path)
		{
			bool absolute = path.StartsWith("/");
			int cwdl = cwd.Length;

			// NOTE: This isn't just a fast path, it handles cases the code below doesn't
			if (!path.StartsWith(".") && path.IndexOf("./") == -1 && path.IndexOf("//") == -1 && !path.EndsWith("."))
			{
				return absolute ? path.Substring(1) : cwdl == 0 ? path : path.Length == 0 ? cwd : cwd + "/" + path;
			}

			char[] @in = new char[path.Length + 1];
			char[] @out = new char[@in.Length + (absolute ? - 1 : cwd.Length)];
			path.CopyTo(0, @in, 0, path.Length - 0);
			int inp = 0, outp = 0;

			if (absolute)
			{
				do
				{
					inp++;
				} while (@in[inp] == '/');
			}
			else if (cwdl != 0)
			{
				cwd.CopyTo(0, @out, 0, cwdl - 0);
				outp = cwdl;
			}

			while (@in[inp] != 0)
			{
				if (inp != 0)
				{
					while (@in[inp] != 0 && @in[inp] != '/')
					{
						@out[outp++] = @in[inp++];
					}
					if (@in[inp] == '\0')
					{
						break;
					}
					while (@in[inp] == '/')
					{
						inp++;
					}
				}

				// Just read a /
				if (@in[inp] == '\0')
				{
					break;
				}
				if (@in[inp] != '.')
				{
					@out[outp++] = '/';
					@out[outp++] = @in[inp++];
					continue;
				}
				// Just read a /.
				if (@in[inp + 1] == '\0' || @in[inp + 1] == '/')
				{
					inp++;
					continue;
				}
				if (@in[inp + 1] == '.' && (@in[inp + 2] == '\0' || @in[inp + 2] == '/')) // ..
				{
					// Just read a /..{$,/}
					inp += 2;
					if (outp > 0)
					{
						outp--;
					}
					while (outp > 0 && @out[outp] != '/')
					{
						outp--;
					}
					//System.err.println("After ..: " + new String(out,0,outp));
					continue;
				}
				// Just read a /.[^.] or /..[^/$]
				inp++;
				@out[outp++] = '/';
				@out[outp++] = '.';
			}
			if (outp > 0 && @out[outp - 1] == '/')
			{
				outp--;
			}
			//System.err.println("normalize: " + path + " -> " + new String(out,0,outp) + " (cwd: " + cwd + ")");
			int outStart = @out[0] == '/' ? 1 : 0;
			return new string(@out,outStart,outp - outStart);
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: FStat hostFStat(final File f, Object data)
		internal virtual FStat hostFStat(File f, object data)
		{
      /*
			bool e = false;
			try
			{
				FileInputStream fis = new FileInputStream(f);
				switch (fis.read())
				{
					case 0x7f: //'\177':
						e = fis.read() == 'E' && fis.read() == 'L' && fis.read() == 'F';
						break;
					case '#':
						e = fis.read() == '!';
					break;
				}
				fis.close();
			}
			catch (IOException e2)
			{
			}
			HostFS fs = (HostFS) data;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int inode = fs.inodes.get(f.getAbsolutePath());
			int inode = fs.inodes.get(f.AbsolutePath);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int devno = fs.devno;
			int devno = fs.devno;
			return new HostFStatAnonymousInnerClassHelper(this, f, e, inode, devno);
   */ throw new NotImplementedException();   
		}

		private class HostFStatAnonymousInnerClassHelper : HostFStat
		{
			private readonly UnixRuntime outerInstance;

			private int _inode;
			private int devno;

			public HostFStatAnonymousInnerClassHelper(UnixRuntime outerInstance, File f, bool e, int inode, int devno) : base(f, e)
			{
				this.outerInstance = outerInstance;
				this._inode = inode;
				this.devno = devno;
			}

			public virtual int inode()
			{
				return _inode;
			}
			public virtual int dev()
			{
				return devno;
			}
		}

		internal virtual FD hostFSDirFD(File f, object _fs)
		{
			HostFS fs = (HostFS) _fs;
			return new org.ibex.nestedvm.UnixRuntime.HostFS.HostDirFD(fs, f);
		}

		public class HostFS : FS
		{
			internal InodeCache inodes = new InodeCache(4000);
			protected internal File root;
			public virtual File Root
			{
				get
				{
					return root;
				}
			}

			protected internal virtual File hostFile(string path)
			{
				char sep = System.IO.Path.DirectorySeparatorChar;
				if (sep != '/')
				{
					char[] buf = path.ToCharArray();
					for (int i = 0;i < buf.Length;i++)
					{
						char c = buf[i];
						if (c == '/')
						{
							buf[i] = sep;
						}
						else if (c == sep)
						{
							buf[i] = '/';
						}
					}
					path = new string(buf);
				}
        throw new NotImplementedException();
				//return new File(root,path);
			}

			public HostFS(string root) : this(new File(root))
			{
			}
			public HostFS(File root)
			{
				this.root = root;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FD open(UnixRuntime r, String path, int flags, int mode) throws ErrnoException
			public override FD open(UnixRuntime r, string path, int flags, int mode)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final File f = hostFile(path);
				File f = hostFile(path);
				return r.hostFSOpen(f,flags,mode,this);
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void unlink(UnixRuntime r, String path) throws ErrnoException
			public override void unlink(UnixRuntime r, string path)
			{
        /*
				File f = hostFile(path);
				if (r.sm != null && !r.sm.allowUnlink(f))
				{
					throw new ErrnoException(EPERM);
				}
				if (!f.exists())
				{
					throw new ErrnoException(ENOENT);
				}
				if (!f.delete())
				{
					// Can't delete file immediately, so mark for
					// delete on close all matching FDs
					bool marked = false;
					for (int i = 0;i < OPEN_MAX;i++)
					{
						if (r.fds[i] != null)
						{
							string fdpath = r.fds[i].NormalizedPath;
							if (fdpath != null && fdpath.Equals(path))
							{
								r.fds[i].markDeleteOnClose();
								marked = true;
							}
						}
					}
					if (!marked)
					{
						throw new ErrnoException(EPERM);
					}
				}
    */ throw new NotImplementedException();    
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FStat stat(UnixRuntime r, String path) throws ErrnoException
			public override FStat stat(UnixRuntime r, string path)
			{
        /*
				File f = hostFile(path);
				if (r.sm != null && !r.sm.allowStat(f))
				{
					throw new ErrnoException(EACCES);
				}
				if (!f.exists())
				{
					return null;
				}
				return r.hostFStat(f,this);
    */ throw new NotImplementedException();    
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void mkdir(UnixRuntime r, String path, int mode) throws ErrnoException
			public override void mkdir(UnixRuntime r, string path, int mode)
			{
        /*
				File f = hostFile(path);
				if (r.sm != null && !r.sm.allowWrite(f))
				{
					throw new ErrnoException(EACCES);
				}
				if (f.exists() && f.Directory)
				{
					throw new ErrnoException(EEXIST);
				}
				if (f.exists())
				{
					throw new ErrnoException(ENOTDIR);
				}
				File parent = getParentFile(f);
				if (parent != null && (!parent.exists() || !parent.Directory))
				{
					throw new ErrnoException(ENOTDIR);
				}
				if (!f.mkdir())
				{
					throw new ErrnoException(EIO);
				}
        */ throw new NotImplementedException();
			}

			internal static File getParentFile(File f)
			{
        /*
				string p = f.Parent;
				return p == null ? null : new File(p);
    */
        throw new NotImplementedException();
			}

			public class HostDirFD : DirFD
			{
        #region implemented abstract members of DirFD

        protected internal override int parinodeode()
        {
          throw new NotImplementedException();
        }

        #endregion

				private readonly UnixRuntime.HostFS outerInstance;

				internal readonly File f;
				internal readonly File[] children;
				public HostDirFD(UnixRuntime.HostFS outerInstance, File f)
				{
          /*
					this.outerInstance = outerInstance;
					this.f = f;
					string[] l = f.list();
					children = new File[l.Length];
					for (int i = 0;i < l.Length;i++)
					{
						children[i] = new File(f,l[i]);
					}
     */ throw new NotImplementedException();     
				}
        protected internal override int size()
				{
					return children.Length;
				}
        protected internal override string name(int n)
				{
          throw new NotImplementedException();
//					return children[n].Name;
				}
				protected internal override int inode(int n)
				{
          throw new NotImplementedException();
					//return outerInstance.inodes.get(children[n].AbsolutePath);
				}
				public virtual int parentInode()
				{
					File parent = getParentFile(f);
          throw new NotImplementedException();
					// HACK: myInode() isn't really correct  if we're not the root
					//return parent == null ? myInode() : outerInstance.inodes.get(parent.AbsolutePath);
				}
				protected internal override int myInode()
				{
          throw new NotImplementedException();

					//return outerInstance.inodes.get(f.AbsolutePath);
				}
				protected internal override int myDev()
				{
					return outerInstance.devno;
				}
			}
		}

		/* Implements the Cygwin notation for accessing MS Windows drive letters
		 * in a unix path. The path /cygdrive/c/myfile is converted to C:\file.
		 * As there is no POSIX standard for this, little checking is done. */
		public class CygdriveFS : HostFS
		{
			protected internal override File hostFile(string path)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char drive = path.charAt(0);
				char drive = path[0];

				if (drive < 'a' || drive > 'z' || path[1] != '/')
				{
					return null;
				}
        throw new NotImplementedException();

			//	path = inode + ":" + path.Substring(1).Replace('/', '\\');
				return new File(path);
			}

			public CygdriveFS() : base("/")
			{
			}
		}

		private static void putInt(sbyte[] buf, int off, int n)//inode)
    {
			buf[off + 0] = unchecked((sbyte)(((int)((uint)n >> 24)) & 0xff));
			buf[off + 1] = unchecked((sbyte)(((int)((uint)n >> 16)) & 0xff));
			buf[off + 2] = unchecked((sbyte)(((int)((uint)n >> 8)) & 0xff));
			buf[off + 3] = unchecked((sbyte)(((int)((uint)n >> 0)) & 0xff));
		}

		public abstract class DirFD : FD
		{
			internal int pos = -2;

			protected internal abstract int size();
			protected internal abstract string name(int n);
			protected internal abstract int inode(int n);
			protected internal abstract int myDev();
			protected internal abstract int parinodeode();
			protected internal abstract int myInode();
			public override int flags()
			{
				return O_RDONLY;
			}

			public virtual int getdents(sbyte[] buf, int off, int len)
			{
        /*
				int ooff = off;
				int ino;
				int reclen;
				for (;len > 0 && pos < size();pos++)
				{
					switch (pos)
					{
						case -2:
						case -1:
							ino = pos == -1 ? parentInode() : myInode();
							if (ino == -1)
							{
								continue;
							}
							reclen = 9 + (pos == -1 ? 2 : 1);
							if (reclen > len)
							{
								goto OUTERBreak;
							}
							buf[off + 8] = '.';
							if (pos == -1)
							{
								buf[off + 9] = '.';
							}
							break;
						default:
						{
							string f = name(pos);
							sbyte[] fb = getBytes(f);
							reclen = fb.Length + 9;
							if (reclen > len)
							{
								goto OUTERBreak;
							}
							ino = inode(pos);
							Array.Copy(fb,0,buf,off + 8,fb.Length);
						}
					break;
					}
					buf[off + reclen - 1] = 0; // null terminate
					reclen = (reclen + 3) & ~3; // add padding
					putInt(buf,off,reclen);
					putInt(buf,off + 4,ino);
					off += reclen;
					len -= reclen;
					OUTERContinue:;
				}
				OUTERBreak:
				return off - ooff;
    */          throw new NotImplementedException();

			}

			protected internal override FStat _fstat()
			{
				return new FStatAnonymousInnerClassHelper(this);
			}

			private class FStatAnonymousInnerClassHelper : FStat
			{
				private readonly DirFD outerInstance;

				public FStatAnonymousInnerClassHelper(DirFD outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				public override int type()
				{
					return S_IFDIR;
				}
				public override int inode()
				{
					return outerInstance.myInode();
				}
				public override int dev()
				{
					return outerInstance.myDev();
				}
			}
		}

		public class DevFS : FS
		{
			internal const int ROOT_INODE = 1;
			internal const int NULL_INODE = 2;
			internal const int ZERO_INODE = 3;
			internal const int FD_INODE = 4;
			internal const int FD_INODES = 32;

			private abstract class DevFStat : FStat
			{
        public DevFStat()
        {
        }
				private readonly UnixRuntime.DevFS outerInstance;

				public DevFStat(UnixRuntime.DevFS outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				public override int dev()
				{
					return outerInstance.devno;
				}
				public override int mode()
				{
					return 0x366;
				}
				public override int type()
				{
					return S_IFCHR;
				}
				public override int nlink()
				{
					return 1;
				}
        public override int inode() { throw new NotImplementedException();}
			}

			private abstract class DevDirFD : DirFD
			{
				private readonly UnixRuntime.DevFS outerInstance;
        public DevDirFD() {}
				public DevDirFD(UnixRuntime.DevFS outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				protected internal override int myDev()
				{
					return outerInstance.devno;
				}
			}

			internal FD devZeroFD = new FDAnonymousInnerClassHelper();

			private class FDAnonymousInnerClassHelper : FD
			{
				public FDAnonymousInnerClassHelper()
				{
				}

				public override int read(sbyte[] a, int off, int length)
				{
					/*Arrays.fill(a,off,off+length,(byte)0);*/
					for (int i = off;i < off + length;i++)
					{
						a[i] = 0;
					}
					return length;
				}
				public override int write(sbyte[] a, int off, int length)
				{
					return length;
				}
				public override int seek(int n, int whence)
				{
					return 0;
				}
				protected internal override FStat _fstat()
				{
					return new DevFStatAnonymousInnerClassHelper(this);
				}

				private class DevFStatAnonymousInnerClassHelper : DevFStat
				{
					private readonly FDAnonymousInnerClassHelper outerInstance;

					public DevFStatAnonymousInnerClassHelper(FDAnonymousInnerClassHelper outerInstance)
					{
						this.outerInstance = outerInstance;
					}

					public override int inode()
					{
						return ZERO_INODE;
					}
				}
				public override int flags()
				{
					return O_RDWR;
				}
			}
			internal FD devNullFD = new FDAnonymousInnerClassHelper2();

			private class FDAnonymousInnerClassHelper2 : FD
			{
				public FDAnonymousInnerClassHelper2()
				{
				}

        public override int read(sbyte[] a, int off, int length)
				{
					return 0;
				}
        public override int write(sbyte[] a, int off, int length)
				{
					return length;
				}
        public override int seek(int n, int whence)
				{
					return 0;
				}
				protected internal override FStat _fstat()
				{
					return new DevFStatAnonymousInnerClassHelper2(this);
				}

				private class DevFStatAnonymousInnerClassHelper2 : DevFStat
				{
					private readonly FDAnonymousInnerClassHelper2 outerInstance;

					public DevFStatAnonymousInnerClassHelper2(FDAnonymousInnerClassHelper2 outerInstance)
					{
						this.outerInstance = outerInstance;
					}

					public override int inode()
					{
						return NULL_INODE;
					}
				}
        public override int flags()
				{
					return O_RDWR;
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FD open(UnixRuntime r, String path, int mode, int flags) throws ErrnoException
			public override FD open(UnixRuntime r, string path, int mode, int flags)
			{
				if (path.Equals("null"))
				{
					return devNullFD;
				}
				if (path.Equals("zero"))
				{
					return devZeroFD;
				}
				if (path.StartsWith("fd/"))
				{
					int n=0;
					try
					{
						n = Convert.ToInt32(path.Substring(3));
					}
					catch (Exception e)
					{
						return null;
					}
					if (n < 0 || n >= OPEN_MAX)
					{
						return null;
					}
					if (r.fds[n] == null)
					{
						return null;
					}
					return r.fds[n].dup();
				}
				if (path.Equals("fd"))
				{
					int count = 0;
					for (int i = 0;i < OPEN_MAX;i++)
					{
						if (r.fds[i] != null)
						{
							count++;
						}
					}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] files = new int[count];
					int[] files = new int[count];
					count = 0;
					for (int i = 0;i < OPEN_MAX;i++)
					{
						if (r.fds[i] != null)
						{
							files[count++] = i;
						}
					}
					return new DevDirFDAnonymousInnerClassHelper(this, files);
				}
				if (path.Equals(""))
				{
					return new DevDirFDAnonymousInnerClassHelper2(this);
				}
				return null;
			}

			private class DevDirFDAnonymousInnerClassHelper : DevDirFD
			{
        #region implemented abstract members of DirFD

        protected internal override int parinodeode()
        {
          throw new NotImplementedException();
        }

        #endregion

				private readonly DevFS outerInstance;

				private int[] files;

				public DevDirFDAnonymousInnerClassHelper(DevFS outerInstance, int[] files) : base(outerInstance)
				{
					this.outerInstance = outerInstance;
					this.files = files;
				}

				protected internal override int myInode()
				{
					return FD_INODE;
				}
        protected internal virtual int parentInode()
				{
					return ROOT_INODE;
				}
        protected internal override int inode(int n)
				{
					return FD_INODES + n;
				}
        protected internal override string name(int n)
				{
					return Convert.ToString(files[n]);
				}
        protected internal override int size()
				{
					return files.Length;
				}
			}

			private class DevDirFDAnonymousInnerClassHelper2 : DevDirFD
			{
        #region implemented abstract members of DirFD

        protected internal override int parinodeode()
        {
          throw new NotImplementedException();
        }

        #endregion

				private readonly DevFS outerInstance;

				public DevDirFDAnonymousInnerClassHelper2(DevFS outerInstance) : base(outerInstance)
				{
					this.outerInstance = outerInstance;
				}

        protected internal override int myInode()
				{
					return ROOT_INODE;
				}
				// HACK: We don't have any clean way to get the parent inode
        protected internal virtual int parentInode()
				{
					return ROOT_INODE;
				}
        protected internal override int inode(int n)
				{
					switch (n)
					{
						case 0:
							return NULL_INODE;
						case 1:
							return ZERO_INODE;
						case 2:
							return FD_INODE;
						default:
							return -1;
					}
				}

        protected internal override string name(int n)
				{
					switch (n)
					{
						case 0:
							return "null";
						case 1:
							return "zero";
						case 2:
							return "fd";
						default:
							return null;
					}
				}
        protected internal override int size()
				{
					return 3;
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FStat stat(UnixRuntime r,String path) throws ErrnoException
			public override FStat stat(UnixRuntime r, string path)
			{
				if (path.Equals("null"))
				{
					return devNullFD.fstat();
				}
				if (path.Equals("zero"))
				{
					return devZeroFD.fstat();
				}
				if (path.StartsWith("fd/"))
				{
					int n;
					try
					{
						n = Convert.ToInt32(path.Substring(3));
					}
					catch (Exception e)
					{
						return null;
					}
					if (n < 0 || n >= OPEN_MAX)
					{
						return null;
					}
					if (r.fds[n] == null)
					{
						return null;
					}
					return r.fds[n].fstat();
				}
				if (path.Equals("fd"))
				{
					return new FStatAnonymousInnerClassHelper(this);
				}
				if (path.Equals(""))
				{
					return new FStatAnonymousInnerClassHelper2(this);
				}
				return null;
			}

			private class FStatAnonymousInnerClassHelper : FStat
			{
				private readonly DevFS outerInstance;

				public FStatAnonymousInnerClassHelper(DevFS outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				public override int inode()
				{
					return FD_INODE;
				}
				public override int dev()
				{
					return outerInstance.devno;
				}
				public override int type()
				{
					return S_IFDIR;
				}
				public override int mode()
				{
					return 0x244;
				}
			}

			private class FStatAnonymousInnerClassHelper2 : FStat
			{
				private readonly DevFS outerInstance;

				public FStatAnonymousInnerClassHelper2(DevFS outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				public override int inode()
				{
					return ROOT_INODE;
				}
				public override int dev()
				{
					return outerInstance.devno;
				}
				public override int type()
				{
					return S_IFDIR;
				}
				public override int mode()
				{
					return 0x244;
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void mkdir(UnixRuntime r, String path, int mode) throws ErrnoException
			public override void mkdir(UnixRuntime r, string path, int mode)
			{
				throw new ErrnoException(EROFS);
			}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void unlink(UnixRuntime r, String path) throws ErrnoException
			public override void unlink(UnixRuntime r, string path)
			{
				throw new ErrnoException(EROFS);
			}
		}


		public class ResourceFS : FS
		{
			internal readonly InodeCache inodes = new InodeCache(500);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FStat lstat(UnixRuntime r, String path) throws ErrnoException
			public override FStat lstat(UnixRuntime r, string path)
			{
				return stat(r,path);
			}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void mkdir(UnixRuntime r, String path, int mode) throws ErrnoException
			public override void mkdir(UnixRuntime r, string path, int mode)
			{
				throw new ErrnoException(EROFS);
			}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void unlink(UnixRuntime r, String path) throws ErrnoException
			public override void unlink(UnixRuntime r, string path)
			{
				throw new ErrnoException(EROFS);
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: FStat connFStat(final URLConnection conn)
			internal virtual FStat connFStat(HttpWebRequest conn)
			{
				return new FStatAnonymousInnerClassHelper(this, conn);
			}

			private class FStatAnonymousInnerClassHelper : FStat
			{
				private readonly ResourceFS outerInstance;

        private HttpWebRequest conn;

        public FStatAnonymousInnerClassHelper(ResourceFS outerInstance, HttpWebRequest conn)
				{
					this.outerInstance = outerInstance;
					this.conn = conn;
				}

				public override int type()
				{
					return S_IFREG;
				}
        public override int nlink()
				{
					return 1;
				}
        public override int mode()
				{
					return 0x244;
				}
        public override int size()
				{
					return (int)conn.ContentLength;
				}
        public override int mtime()
				{
          throw new NotImplementedException();
          //					return (int)(conn.Date / 1000);
				}
        public override int inode()
				{
          throw new NotImplementedException();
          //return outerInstance.inodes.get(conn.URL.ToString());
				}
        public override int dev()
				{
					return outerInstance.devno;
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FStat stat(UnixRuntime r, String path) throws ErrnoException
			public override FStat stat(UnixRuntime r, string path)
			{
        /*
				URL url = r.GetType().getResource("/" + path);
				if (url == null)
				{
					return null;
				}
				try
				{
					return connFStat(url.openConnection());
				}
				catch (IOException e)
				{
					throw new ErrnoException(EIO);
				}
    */
        throw new NotImplementedException();

			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FD open(UnixRuntime r, String path, int flags, int mode) throws ErrnoException
			public override FD open(UnixRuntime r, string path, int flags, int mode)
			{
        /*
				if ((flags & ~3) != 0)
				{
					if (STDERR_DIAG)
					{
						Console.Error.WriteLine("WARNING: Unsupported flags passed to ResourceFS.open(\"" + path + "\"): " + toHex(flags & ~3));
					}
					throw new ErrnoException(ENOTSUP);
				}
				if ((flags & 3) != RD_ONLY)
				{
					throw new ErrnoException(EROFS);
				}
				URL url = r.GetType().getResource("/" + path);
				if (url == null)
				{
					return null;
				}
				try
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final URLConnection conn = url.openConnection();
					URLConnection conn = url.openConnection();
					Seekable.InputStream si = new Seekable.InputStream(conn.InputStream);
					return new SeekableFDAnonymousInnerClassHelper(this, si, flags, conn);
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
    */          throw new NotImplementedException();

			}

			private class SeekableFDAnonymousInnerClassHelper : SeekableFD
			{
				private readonly ResourceFS outerInstance;

				private HttpWebRequest conn;

				public SeekableFDAnonymousInnerClassHelper(ResourceFS outerInstance, InputStream si, int flags, HttpWebRequest conn) : base(si, flags)
				{
					this.outerInstance = outerInstance;
					this.conn = conn;
				}

				protected internal override FStat _fstat()
				{
					return outerInstance.connFStat(conn);
				}
			}
		}
	}
}