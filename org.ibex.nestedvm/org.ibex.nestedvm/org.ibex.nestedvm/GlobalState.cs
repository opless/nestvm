using System;
using System.Collections;
using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
    public sealed class GlobalState
    {
        internal Hashtable execCache = new Hashtable();
    
        internal readonly UnixRuntime[] tasks;
        internal int nextPID = 1;
    
        /// <summary>
        /// Table of all current file locks held by this process. </summary>
        internal Lock[] locks = new Lock[16];
    
        internal MP[] mps = new MP[0];
        internal org.ibex.nestedvm.UnixRuntime.FS root;
    
        public GlobalState() : this(255)
        {
        }
        public GlobalState(int maxProcs) : this(maxProcs,true)
        {
        }
        public GlobalState(int maxProcs, bool defaultMounts)
        {
            /*
      tasks = new UnixRuntime[maxProcs + 1];
      if (defaultMounts)
      {
        File root = null;
        if (Platform.getProperty("nestedvm.root") != null)
        {
          root = new File(Platform.getProperty("nestedvm.root"));
          if (!root.Directory)
          {
            throw new System.ArgumentException("nestedvm.root is not a directory");
          }
        }
        else
        {
          string cwd = Platform.getProperty("user.dir");
          root = Platform.getRoot(new File(cwd != null ? cwd : "."));
        }
        
        addMount("/",new HostFS(root));
        
        if (Platform.getProperty("nestedvm.root") == null)
        {
          File[] roots = Platform.listRoots();
          for (int i = 0;i < roots.Length;i++)
          {
            string name = roots[i].Path;
            if (name.EndsWith(File.separator))
            {
              name = name.Substring(0,name.Length - 1);
            }
            if (name.Length == 0 || name.IndexOf('/') != -1)
            {
              continue;
            }
            addMount("/" + name.ToLower(),new HostFS(roots[i]));
          }
        }
        
        addMount("/dev",new DevFS());
        addMount("/resource",new ResourceFS());
        addMount("/cygdrive",new CygdriveFS());
        
      }
      */          throw new NotImplementedException();

        }
    
        public string mapHostPath(string s)
        {
            return mapHostPath(new File(s));
        }
        public string mapHostPath(File f)
        {
            /*
      MP[] list;
      FS root;
      lock (this)
      {
        mps = this.mps;
        root = this.root;
      }
      if (!f.Absolute)
      {
        f = new File(f.AbsolutePath);
      }
      for (int i = mps.Length;i >= 0;i--)
      {
        FS fs = i == mps.Length ? root : mps[i].fs;
        string path = i == mps.Length ? "" : mps[i].path;
        if (!(fs is HostFS))
        {
          continue;
        }
        File fsroot = ((HostFS)fs).Root;
        if (!fsroot.Absolute)
        {
          fsroot = new File(fsroot.AbsolutePath);
        }
        if (f.Path.StartsWith(fsroot.Path))
        {
          char sep = System.IO.Path.DirectorySeparatorChar;
          string child = f.Path.Substring(fsroot.Path.length());
          if (sep != '/')
          {
            char[] child_ = child.ToCharArray();
            for (int j = 0;j < child_.Length;j++)
            {
              if (child_[j] == '/')
              {
                child_[j] = sep;
              }
              else if (child_[j] == sep)
              {
                child_[j] = '/';
              }
            }
            child = new string(child_);
          }
          string mapped = "/" + (path.Length == 0?"":path + "/") + child;
          return mapped;
        }
      }
      return null;
      */ throw new NotImplementedException();
        }
    
        internal class MP : Sort.Comparable
        {
            public MP(string path, org.ibex.nestedvm.UnixRuntime.FS fs)
            {
                this.path = path;
                this.fs = fs;
            }
            public string path;
            public org.ibex.nestedvm.UnixRuntime.FS fs;
            public virtual int compareTo(object o)
            {
                if (!(o is MP))
                {
                    return 1;
                }
                return -path.CompareTo(((MP)o).path);
            }
        }
    
        public org.ibex.nestedvm.UnixRuntime.FS getMount(string path)
        {
            lock (this)
            {
                if (!path.StartsWith("/"))
                {
                    throw new System.ArgumentException("Mount point doesn't start with a /");
                }
                if (path.Equals("/"))
                {
                    return root;
                }
                path = path.Substring(1);
                for (int i = 0;i < mps.Length;i++)
                {
                    if (mps[i].path.Equals(path))
                    {
                        return mps[i].fs;
                    }
                }
                return null;
            }
        }
    
        public void addMount(string path, org.ibex.nestedvm.UnixRuntime.FS fs)
        {
            /*
      lock (this)
      {
        if (getMount(path) != null)
        {
          throw new System.ArgumentException("mount point already exists");
        }
        if (!path.StartsWith("/"))
        {
          throw new System.ArgumentException("Mount point doesn't start with a /");
        }
        
        if (fs.owner != null)
        {
          fs.owner.removeMount(fs);
        }
        fs.owner = this;
        
        if (path.Equals("/"))
        {
          root = fs;
          fs.devno = 1;
          return;
        }
        path = path.Substring(1);
        int oldLength = mps.Length;
        MP[] newMPS = new MP[oldLength + 1];
        if (oldLength != 0)
        {
          Array.Copy(mps,0,newMPS,0,oldLength);
        }
        newMPS[oldLength] = new MP(path,fs);
        Sort.sort(newMPS);
        mps = newMPS;
        int highdevno = 0;
        for (int i = 0;i < mps.Length;i++)
        {
          highdevno = max(highdevno,mps[i].fs.devno);
        }
        fs.devno = highdevno + 2;
      }
      */           throw new NotImplementedException();

        }
    
        public void removeMount(org.ibex.nestedvm.UnixRuntime.FS fs)
        {
            lock (this)
            {
                for (int i = 0;i < mps.Length;i++)
                {
                    if (mps[i].fs == fs)
                    {
                        removeMount(i);
                        return;
                    }
                }
                throw new System.ArgumentException("mount point doesn't exist");
            }
        }
    
        public void removeMount(string path)
        {
            lock (this)
            {
                if (!path.StartsWith("/"))
                {
                    throw new System.ArgumentException("Mount point doesn't start with a /");
                }
                if (path.Equals("/"))
                {
                    removeMount(-1);
                }
                else
                {
                    path = path.Substring(1);
                    int p;
                    for (p = 0;p < mps.Length;p++)
                    {
                        if (mps[p].path.Equals(path))
                        {
                            break;
                        }
                    }
                    if (p == mps.Length)
                    {
                        throw new System.ArgumentException("mount point doesn't exist");
                    }
                    removeMount(p);
                }
            }
        }
    
        internal void removeMount(int index)
        {
            if (index == -1)
            {
                root.owner = null;
                root = null;
                return;
            }
            MP[] newMPS = new MP[mps.Length - 1];
            Array.Copy(mps,0,newMPS,0,index);
            Array.Copy(mps,0,newMPS,index,mps.Length - index - 1);
            mps = newMPS;
        }
    
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private Object fsop(int op, UnixRuntime r, String normalizedPath, int arg1, int arg2) throws ErrnoException
        internal object fsop(int op, UnixRuntime r, string normalizedPath, int arg1, int arg2)
        {
            int pl = normalizedPath.Length;
            if (pl != 0)
            {
                MP[] list;
                lock (this)
                {
                    list = mps;
                }
                for (int i = 0;i < list.Length;i++)
                {
                    MP mp = list[i];
                    int mpl = mp.path.Length;
                    if (normalizedPath.StartsWith(mp.path) && (pl == mpl || normalizedPath[mpl] == '/'))
                    {
                        return mp.fs.dispatch(op,r,pl == mpl ? "" : normalizedPath.Substring(mpl + 1),arg1,arg2);
                    }
                }
            }
            return root.dispatch(op,r,normalizedPath,arg1,arg2);
        }
    
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public final FD open(UnixRuntime r, String path, int flags, int mode) throws ErrnoException
        public org.ibex.nestedvm.FD open(UnixRuntime r, string path, int flags, int mode)
        {
            throw new NotImplementedException();

            //return (org.ibex.nestedvm.Runtime.FD) fsop(org.ibex.nestedvm.Runtime.FS.OPEN,r,path,flags,mode);
        }
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public final FStat stat(UnixRuntime r, String path) throws ErrnoException
        public org.ibex.nestedvm.FStat stat(UnixRuntime r, string path)
        {
            throw new NotImplementedException();

            //return (org.ibex.nestedvm.Runtime.FStat) fsop(org.ibex.nestedvm.Runtime.FS.STAT,r,path,0,0);
        }
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public final FStat lstat(UnixRuntime r, String path) throws ErrnoException
        public org.ibex.nestedvm.FStat lstat(UnixRuntime r, string path)
        {
            throw new NotImplementedException();

            //return (org.ibex.nestedvm.Runtime.FStat) fsop(org.ibex.nestedvm.Runtime.FS.LSTAT,r,path,0,0);
        }
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public final void mkdir(UnixRuntime r, String path, int mode) throws ErrnoException
        public void mkdir(UnixRuntime r, string path, int mode)
        {
            throw new NotImplementedException();

            //fsop(org.ibex.nestedvm.Runtime.FS.MKDIR,r,path,mode,0);
        }
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public final void unlink(UnixRuntime r, String path) throws ErrnoException
        public void unlink(UnixRuntime r, string path)
        {
            throw new NotImplementedException();

            //fsop(org.ibex.nestedvm.Runtime.FS.UNLINK,r,path,0,0);
        }
    
        internal class CacheEnt
        {
            public readonly long time;
            public readonly long size;
            public readonly object o;
            public CacheEnt(long time, long size, object o)
            {
                this.time = time;
                this.size = size;
                this.o = o;
            }
        }
    }
}