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
    #endregion
  }

}

