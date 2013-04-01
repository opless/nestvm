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
    public int Open(MipsInterpreter interp, int nameAddr, int oflag, int mode)
    {
      throw new NotImplementedException();
    }
    public int Close(MipsInterpreter interp, int fd)
    {
      throw new NotImplementedException();
    }
    public int Write(MipsInterpreter interp, int fd, int addr, int count)
    {
      throw new NotImplementedException();
    }
    public int Read(MipsInterpreter interp, int fd, int addr, int count)
    {
      throw new NotImplementedException();
    }
    public int FStat(MipsInterpreter interp, int fd, int buffAddr)
    {
      throw new NotImplementedException();
    }
    public int LSeek(MipsInterpreter interp, int fd, int offset, int whence)
    {
      throw new NotImplementedException();
    }
    public int FTruncate(MipsInterpreter interp, int fd, int length)
    {
      throw new NotImplementedException();
    }
    public int FSync(MipsInterpreter interp, int fd)
    {
      throw new NotImplementedException();
    }
    public int FCntl(MipsInterpreter interp, int fd, int cmd, int arg)
    {
      throw new NotImplementedException();
    }
    #endregion
  }

}

