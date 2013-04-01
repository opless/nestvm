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
  public class BasicProcessManager : IProcessManager
  {
    public BasicProcessManager()
    {
    }

    #region IProcessManager implementation

    public int GetProcessId(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    public int Exit(ICpuInterpreter interp, int value)
    {
      throw new NotImplementedException();
    }

    public int UMask(int mode)
    {
      throw new NotImplementedException();
    }

    public int SysCtl(ICpuInterpreter interp, int nameAddr, int nameLen, int oldP, int oldLenAddr, int newP, int newLen)
    {
      throw new NotImplementedException();
    }

    public int GetPPid(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    public int ChDir(ICpuInterpreter interp, int cstringArg)
    {
      throw new NotImplementedException();
    }

    public int GetCwd(ICpuInterpreter interp, int addr, int size)
    {
      throw new NotImplementedException();
    }

    public int Fork(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    public int Kill(ICpuInterpreter interp, int pid, int signal)
    {
      throw new NotImplementedException();
    }

    public int GetEffectiveGroupId(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    public int GetGroupId(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    public int GetEffectiveUserId(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    public int GetUserId(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}

