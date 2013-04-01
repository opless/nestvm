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
using System.Collections.Generic;

namespace org.ibex.nestedvm
{
  public interface IInOutStream
  {

    bool CanRead();
    bool CanWrite();
    bool CanSeek();

    void Close();

    int Write(byte[] source, int offset, int count);
    int Read(byte[] destination, int offset, int count);

    void Flush();

    long GetLength();

    void Seek(long position, int seekFrom);

  }


}

