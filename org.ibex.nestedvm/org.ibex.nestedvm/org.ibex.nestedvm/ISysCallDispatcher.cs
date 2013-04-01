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
  public interface ISysCallDispatcher
  {
    int Dispatch(MipsInterpreter interp,int syscall,int a, int b, int c, int d, int e, int f);
  }

}

