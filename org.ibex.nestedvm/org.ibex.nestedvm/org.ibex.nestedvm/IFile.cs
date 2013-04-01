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
  public interface IFile
  {
    int Read(byte[] buff, int offset, int length);
    int Write(byte[] buff, int offset, int length);
    bool Close();
    bool Sync();
    bool IsTty();
  }



}

