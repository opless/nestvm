//
//  Author:
//    simon simon@simonwaite.com
//
//  Copyright (c) 2013, Simon Waite
//
//  All rights reserved.
//
//
using System;

namespace org.ibex.nestedvm
{
  class BasicVirtualFileSystemImplementation : IVirtualFileSystem
  {
    #region IVirtualFileSystem implementation
    public int Open(ICpuInterpreter interp, int nameAddr, int oflag, int mode)
    {
      throw new NotImplementedException();
    }
    public int Close(ICpuInterpreter interp, int fd)
    {
      throw new NotImplementedException();
    }
    public int Write(ICpuInterpreter interp, int fd, int addr, int count)
    {
      throw new NotImplementedException();
    }
    public int Read(ICpuInterpreter interp, int fd, int addr, int count)
    {
      throw new NotImplementedException();
    }
    public int FStat(ICpuInterpreter interp, int fd, int buffAddr)
    {
      throw new NotImplementedException();
    }
    public int LSeek(ICpuInterpreter interp, int fd, int offset, int whence)
    {
      throw new NotImplementedException();
    }
    public int FTruncate(ICpuInterpreter interp, int fd, int length)
    {
      throw new NotImplementedException();
    }
    public int FSync(ICpuInterpreter interp, int fd)
    {
      throw new NotImplementedException();
    }
    public int FCntl(ICpuInterpreter interp, int fd, int cmd, int arg)
    {
      throw new NotImplementedException();
    }

    public int FChmod(ICpuInterpreter interp, int fd, int mode)
    {
      throw new NotImplementedException();
    }

    public int Chmod(ICpuInterpreter interp, int cStringAddr, int mode)
    {
      throw new NotImplementedException();
    }

    public int FChown(ICpuInterpreter interp, int fd, int owner, int group)
    {
      throw new NotImplementedException();
    }

    public int LChown(ICpuInterpreter interp, int cStringAddr, int owner, int group)
    {
      throw new NotImplementedException();
    }

    public int Chown(ICpuInterpreter interp, int cStringAddr, int owner, int group)
    {
      throw new NotImplementedException();
    }

    public int RealPath(ICpuInterpreter interp, int inAddr, int outAddr)
    {
      throw new NotImplementedException();
    }

    public int Access(ICpuInterpreter interp, int cstringArg, int mode)
    {
      throw new NotImplementedException();
    }

    public int Unlink(ICpuInterpreter interp, int cstringArg)
    {
      throw new NotImplementedException();
    }

    public int MkDir(ICpuInterpreter interp, int cstringArg, int mode)
    {
      throw new NotImplementedException();
    }

    public int LStat(ICpuInterpreter interp, int cstringArg, int addr)
    {
      throw new NotImplementedException();
    }

    public int Stat(ICpuInterpreter interp, int cstringArg, int addr)
    {
      throw new NotImplementedException();
    }

    public int Dup(ICpuInterpreter interp, int fd)
    {
      throw new NotImplementedException();
    }

    public int Dup2(ICpuInterpreter interp, int fda, int fdb)
    {
      throw new NotImplementedException();
    }

    public int Pipe(ICpuInterpreter interp, int addr)
    {
      throw new NotImplementedException();
    }

    #endregion
  }

}

