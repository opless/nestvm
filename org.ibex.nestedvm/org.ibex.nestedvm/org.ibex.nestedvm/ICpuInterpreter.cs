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
  public interface ICpuInterpreter
  {
    IVirtualFileSystem GetVirtFS();

    IVirtualMemory GetVirtMem();

    IProcessManager GetProcMgr();
  }
}

