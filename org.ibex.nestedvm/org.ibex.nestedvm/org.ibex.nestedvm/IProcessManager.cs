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
    int GetEffectiveGroupId(MipsInterpreter interp);

    int GetGroupId(MipsInterpreter interp);

    int GetEffectiveUserId(MipsInterpreter interp);

    int GetUserId(MipsInterpreter interp);
  }
}

