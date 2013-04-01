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
  class BasicVirtualMemoryImplementation : IVirtualMemory
  {
    #region IVirtualMemory implementation

    public int Read(int address)
    {
      throw new NotImplementedException();
    }

    public void Write(int address, int value)
    {
      throw new NotImplementedException();
    }


    public void MemCpy(int destination, int source, int count)
    {
      throw new NotImplementedException();
    }

    public void MemSet(int destination, int value, int count)
    {
      throw new NotImplementedException();
    }

    #endregion
  }

}

