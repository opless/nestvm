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
using org.ibex.nestedvm;

namespace RunMe
{
  class MainClass
  {
    public static void Main(string[] args)
    {
      ICpuInterpreter cpu = new MipsInterpreter();

      cpu.LoadImage("test.elf");

     
    }
  }
}
