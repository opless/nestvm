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
  public interface IVirtualMemory
  {

    /// <summary>
    /// Read the specified address.
    /// </summary>
    /// <param name="address">Address.</param>
    int Read(int address);
    /// <summary>
    /// Write the specified address and value.
    /// </summary>
    /// <param name="address">Address.</param>
    /// <param name="value">Value.</param>
    void Write(int address, int value);

    /// <summary>
    /// This Copies count bytes from memory area source to memory area destination.  
    /// If s1 and s2 overlap, behavior is undefined.  
    /// </summary>
    /// <param name="destination">Destination.</param>
    /// <param name="source">Source.</param>
    /// <param name="count">Count.</param>
    void MemCpy(int destination, int source, int count);

    /// <summary>
    /// Sets an area of memory of count bytes starting at destination, 
    /// with value (converted to an unsigned byte)
    /// 
    /// </summary>
    /// <param name="destination">Destination.</param>
    /// <param name="value">Value.</param>
    /// <param name="count">Count.</param>
    void MemSet(int destination, int value, int count);
  }

}

