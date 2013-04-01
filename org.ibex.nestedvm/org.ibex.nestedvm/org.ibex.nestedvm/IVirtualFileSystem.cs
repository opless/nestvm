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
  public interface IVirtualFileSystem
  {
//    IFile Open(string path, int openFlags, int mode);
//    IDirectory OpenDir(string path);

    int Open(MipsInterpreter interp, int nameAddr, int oflag, int mode);

    int Close(MipsInterpreter interp, int fd);

    int Write(MipsInterpreter interp, int fd, int addr, int count);

    int Read(MipsInterpreter interp, int fd, int addr, int count);

    int FStat(MipsInterpreter interp, int fd, int buffAddr);

    int LSeek(MipsInterpreter interp, int fd, int offset, int whence);

    int FTruncate(MipsInterpreter interp, int fd, int length);

    int FSync(MipsInterpreter interp, int fd);

    int FCntl(MipsInterpreter interp, int fd, int cmd, int arg);
  }


}

