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
    int FCntlLock(ICpuInterpreter interp, int fd, int cmd, int arg);

    int FChmod(ICpuInterpreter interp, int fd, int mode);

    int Chmod(ICpuInterpreter interp, int cStringAddr, int mode);

    int FChown(ICpuInterpreter interp, int fd, int owner, int group);

    int LChown(ICpuInterpreter interp, int cStringAddr, int owner, int group);

    int Chown(ICpuInterpreter interp, int cStringAddr, int owner, int group);

    int RealPath(ICpuInterpreter interp, int inAddr, int outAddr);

    int Access(ICpuInterpreter interp, int cstringArg, int mode);

    int Unlink(ICpuInterpreter interp, int cstringArg);

    int MkDir(ICpuInterpreter interp, int cstringArg, int mode);

    int LStat(ICpuInterpreter interp, int cstringArg, int addr);

    int Stat(ICpuInterpreter interp, int cstringArg, int addr);

    int Dup(ICpuInterpreter interp, int fd);

    int Dup2(ICpuInterpreter interp, int fda, int fdb);

    int Pipe(ICpuInterpreter interp, int addr);

//    IFile Open(string path, int openFlags, int mode);
//    IDirectory OpenDir(string path);

    int Open(ICpuInterpreter interp, int nameAddr, int oflag, int mode);

    int Close(ICpuInterpreter interp, int fd);

    int Write(ICpuInterpreter interp, int fd, int addr, int count);

    int Read(ICpuInterpreter interp, int fd, int addr, int count);

    int FStat(ICpuInterpreter interp, int fd, int buffAddr);

    int LSeek(ICpuInterpreter interp, int fd, int offset, int whence);

    int FTruncate(ICpuInterpreter interp, int fd, int length);

    int FSync(ICpuInterpreter interp, int fd);

    int FCntl(ICpuInterpreter interp, int fd, int cmd, int arg);
  }


}

