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
  public interface IProcessManager
  {
    int UMask(int mode);

    int SysCtl(ICpuInterpreter interp, int nameAddr, int nameLen, int oldP, int oldLenAddr, int newP, int newLen);

    int GetPPid(ICpuInterpreter interp);

    int ChDir(ICpuInterpreter interp, int cstringArg);

    int GetCwd(ICpuInterpreter interp, int addr, int size);

    int Fork(ICpuInterpreter interp);

    int Kill(ICpuInterpreter interp, int pid, int signal);

    int GetEffectiveGroupId(ICpuInterpreter interp);

    int GetGroupId(ICpuInterpreter interp);

    int GetEffectiveUserId(ICpuInterpreter interp);

    int GetUserId(ICpuInterpreter interp);
  }
}

