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
using org.ibex.nestedvm.util;

namespace org.ibex.nestedvm
{
  public class MipsInterpreter : ICpuInterpreter
  {

    public void LoadImage(string filename)
    {
      Console.WriteLine("Loading \"{0}\"", filename);

      ELF elf = new ELF(filename);
      if (elf.header.type != ELF.ET_EXEC)
      {
        throw new ArgumentException("Not an Executable", "filename");
      }
      if (elf.header.machine != ELF.EM_MIPS)
      {
        throw new ArgumentException("Not an MIPS Executable", "filename");
      }
      if (elf.ident.data != ELF.ELFDATA2MSB)
      {
        throw new ArgumentException("Binary is not big endian");
      }

      ELF.Symbol gpsym = elf.Symtab.getGlobalSymbol("_gp");
      
      if (gpsym == null)
      {
        throw new ArgumentException("NO _gp symbol!");
      }
      int gp = gpsym.addr;

      int phId = 0;
      foreach (var ph in elf.pheaders)
      {
        phId ++;
        if (ph.type != ELF.PT_LOAD)
        {
          Console.WriteLine("Skipping Program Header {0}", phId);
          continue;
        }
        Console.WriteLine("Loading Program Header {0}", phId);

        InputStream inp = ph.InputStream;

        for (int i=0; i< ph.filesz; i++)
        {
          int x = inp.read() << 24;
          x += inp.read() << 16;
          x += inp.read() << 8;
          x += inp.read();
          virtmem.Write(ph.vaddr + i, x);
          switch (i % 4)
          {
            case 0:
              Console.Write("-");
              break;
            case 1:
              Console.Write("/");
              break;
            case 2:
              Console.Write("|");
              break;
            case 3:
              Console.Write("\\");
              break;
          }
          Console.Write("\r");
          Console.Out.Flush();
        }
      


      }

    }


    #region Register Names
    public const int ZERO = 0;
    public const int AT = 1;
    public const int K0 = 26;
    public const int K1 = 27;
    public const int GP = 28;
    public const int SP = 29;
    public const int FP = 30;
    public const int RA = 31;
    public const int V0 = 2;
    public const int V1 = 3;
    public const int A0 = 4;
    public const int A1 = 5;
    public const int A2 = 6;
    public const int A3 = 7;
    public const int T0 = 8;
    public const int T1 = 9;
    public const int T2 = 10;
    public const int T3 = 11;
    public const int T4 = 12;
    public const int T5 = 13;
    public const int T6 = 14;
    public const int T7 = 15;
    public const int T8 = 24;
    public const int T9 = 25;
    public const int S0 = 16;
    public const int S1 = 17;
    public const int S2 = 18;
    public const int S3 = 19;
    public const int S4 = 20;
    public const int S5 = 21;
    public const int S6 = 22;
    public const int S7 = 23;
    #endregion

    int[] registers = new int[32];
    int pc;
    int hi;
    int lo;

//    int pageShift;
    int state=0;
    int RUNNING=0;

    ISysCallDispatcher syscall;
    IVirtualMemory     virtmem;
    IVirtualFileSystem virtfs;
    IProcessManager    procMgr;

    public MipsInterpreter()
    {
      virtmem = new BasicVirtualMemoryImplementation();
      syscall = new BasicSysCallDispatcher();
      virtfs = new BasicVirtualFileSystemImplementation();
      procMgr = new BasicProcessManager();
    }

    public IProcessManager GetProcMgr()
    {
      return procMgr;
    }

    public IVirtualFileSystem GetVirtFS()
    {
      return virtfs;
    }

    public IVirtualMemory GetVirtMem()
    {
      return virtmem;
    }



    int MemRead(int addr)
    {
      return virtmem.Read(addr);
    }

    void MemWrite(int addr, int value)
    {
      virtmem.Write(addr,value);
    }
    /*
     *    /// The syscall dispatcher.
    ///    The should be called by subclasses when the syscall instruction is invoked.
    ///    <i>syscall</i> should be the contents of V0 and <i>a</i>, <i>b</i>, <i>c</i>, and <i>d</i> should be 
    ///    the contenst of A0, A1, A2, and A3. The call MAY change the state </summary>
    ///    <seealso cref= Runtime#state state  </seealso>
/*/

    int SysCall(int sys, int a, int b, int c, int d, int e, int f)
    {
      return syscall.Dispatch(this,sys,a,b,c,d,e,f);
    }



    private int runSome()
    {
      //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
      //ORIGINAL LINE: final int PAGE_WORDS = (1<<pageShift)>>2;
      //int PAGE_WORDS = (1 << pageShift) >> 2;
      int[] r = registers;
      //int[] f = fpregs;
      int pc = this.pc;
      int nextPC = pc + 4;
      try
      {
        for (;;)
        {
          int insn;
          try
          {
            //insn = readPages [(int)((uint)pc >> pageShift)] [((int)((uint)pc >> 2)) & PAGE_WORDS - 1];
            insn = MemRead(pc);
          } catch (Exception e)
          {
            if ((uint)pc == 0xdeadbeef)
            {
              throw new Exception("fell off cpu: r2: " + r [2]);
            }
            insn = MemRead(pc);
          }
          
          int op = ((int)((uint)insn >> 26)) & 0xff; // bits 26-31
          int rs = ((int)((uint)insn >> 21)) & 0x1f; // bits 21-25
          int rt = ((int)((uint)insn >> 16)) & 0x1f; // bits 16-20
          int ft = ((int)((uint)insn >> 16)) & 0x1f;
          int rd = ((int)((uint)insn >> 11)) & 0x1f; // bits 11-15
          int fs = ((int)((uint)insn >> 11)) & 0x1f;
          int shamt = ((int)((uint)insn >> 6)) & 0x1f; // bits 6-10
          int fd = ((int)((uint)insn >> 6)) & 0x1f;
          int subcode = insn & 0x3f; // bits 0-5
          
          int jumpTarget = (insn & 0x03ffffff); // bits 0-25
          int unsignedImmediate = insn & 0xffff;
          int signedImmediate = (insn << 16) >> 16;
          int branchTarget = signedImmediate;
          
          int tmp, addr; // temporaries
          
          r [ZERO] = 0;
          
          switch (op)
          {
            case 0:
            {
              switch (subcode)
              {
                case 0: // SLL
                  if (insn == 0)
                  {
                    break;
                  }
                  r [rd] = r [rt] << shamt;
                  break;
                case 2: // SRL
                  r [rd] = (int)((uint)r [rt] >> shamt);
                  break;
                case 3: // SRA
                  r [rd] = r [rt] >> shamt;
                  break;
                case 4: // SLLV
                  r [rd] = r [rt] << (r [rs] & 0x1f);
                  break;
                case 6: // SRLV
                  r [rd] = (int)((uint)r [rt] >> (r [rs] & 0x1f));
                  break;
                case 7: // SRAV
                  r [rd] = r [rt] >> (r [rs] & 0x1f);
                  break;
                case 8: // JR
                  tmp = r [rs];
                  pc += 4;
                  nextPC = tmp;
                  goto OUTERContinue;
                case 9: // JALR
                  tmp = r [rs];
                  pc += 4;
                  r [rd] = pc + 4;
                  nextPC = tmp;
                  goto OUTERContinue;
                case 12: // SYSCALL
                  this.pc = pc;
                  r [V0] = SysCall(r [V0], r [A0], r [A1], r [A2], r [A3], r [T0], r [T1]);
                  if (state != RUNNING)
                  {
                    this.pc = nextPC;
                    goto OUTERBreak;
                  }
                  break;
                case 13: // BREAK
                  throw new ExecutionException("Break");
                case 16: // MFHI
                  r [rd] = hi;
                  break;
                case 17: // MTHI
                  hi = r [rs];
                  break;
                case 18: // MFLO
                  r [rd] = lo;
                  break;
                case 19: // MTLO
                  lo = r [rs];
                  break;
                case 24: // MULT
                {
                  long hilo = ((long)r [rs]) * ((long)r [rt]);
                  hi = (int)((long)((ulong)hilo >> 32));
                  lo = (int)hilo;
                  break;
                }
                case 25: // MULTU
                {
                  long hilo = (r [rs] & 0xffffffffL) * (r [rt] & 0xffffffffL);
                  hi = (int)((long)((ulong)hilo >> 32));
                  lo = (int)hilo;
                  break;
                }
                case 26: // DIV
                  hi = r [rs] % r [rt];
                  lo = r [rs] / r [rt];
                  break;
                case 27: // DIVU
                  if (rt != 0)
                  {
                    hi = (int)((r [rs] & 0xffffffffL) % (r [rt] & 0xffffffffL));
                    lo = (int)((r [rs] & 0xffffffffL) / (r [rt] & 0xffffffffL));
                  }
                  break;
                case 32: // ADD
                  throw new ExecutionException("ADD (add with oveflow trap) not suported");
                  /*This must trap on overflow
              r[rd] = r[rs] + r[rt];
              break;*/
                case 33: // ADDU
                  r [rd] = r [rs] + r [rt];
                  break;
                case 34: // SUB
                  throw new ExecutionException("SUB (sub with oveflow trap) not suported");
                  /*This must trap on overflow
              r[rd] = r[rs] - r[rt];
              break;*/
                case 35: // SUBU
                  r [rd] = r [rs] - r [rt];
                  break;
                case 36: // AND
                  r [rd] = r [rs] & r [rt];
                  break;
                case 37: // OR
                  r [rd] = r [rs] | r [rt];
                  break;
                case 38: // XOR
                  r [rd] = r [rs] ^ r [rt];
                  break;
                case 39: // NOR
                  r [rd] = ~(r [rs] | r [rt]);
                  break;
                case 42: // SLT
                  r [rd] = r [rs] < r [rt] ? 1 : 0;
                  break;
                case 43: // SLTU
                  r [rd] = ((r [rs] & 0xffffffffL) < (r [rt] & 0xffffffffL)) ? 1 : 0;
                  break;
                default:
                  throw new ExecutionException("Illegal instruction 0/" + subcode);
              }
              break;
            }
            case 1:
            {
              switch (rt)
              {
                case 0: // BLTZ
                  if (r [rs] < 0)
                  {
                    pc += 4;
                    tmp = pc + branchTarget * 4;
                    nextPC = tmp;
                    goto OUTERContinue;
                  }
                  break;
                case 1: // BGEZ
                  if (r [rs] >= 0)
                  {
                    pc += 4;
                    tmp = pc + branchTarget * 4;
                    nextPC = tmp;
                    goto OUTERContinue;
                  }
                  break;
                case 16: // BLTZAL
                  if (r [rs] < 0)
                  {
                    pc += 4;
                    r [RA] = pc + 4;
                    tmp = pc + branchTarget * 4;
                    nextPC = tmp;
                    goto OUTERContinue;
                  }
                  break;
                case 17: // BGEZAL
                  if (r [rs] >= 0)
                  {
                    pc += 4;
                    r [RA] = pc + 4;
                    tmp = pc + branchTarget * 4;
                    nextPC = tmp;
                    goto OUTERContinue;
                  }
                  break;
                default:
                  throw new ExecutionException("Illegal Instruction");
              }
              break;
            }
            case 2: // J
            {
              tmp = (pc & unchecked((int)0xf0000000)) | (jumpTarget << 2);
              pc += 4;
              nextPC = tmp;
              goto OUTERContinue;
            }
            case 3: // JAL
            {
              tmp = (pc & unchecked((int)0xf0000000)) | (jumpTarget << 2);
              pc += 4;
              r [RA] = pc + 4;
              nextPC = tmp;
              goto OUTERContinue;
            }
            case 4: // BEQ
              if (r [rs] == r [rt])
              {
                pc += 4;
                tmp = pc + branchTarget * 4;
                nextPC = tmp;
                goto OUTERContinue;
              }
              break;
            case 5: // BNE
              if (r [rs] != r [rt])
              {
                pc += 4;
                tmp = pc + branchTarget * 4;
                nextPC = tmp;
                goto OUTERContinue;
              }
              break;
            case 6: //BLEZ
              if (r [rs] <= 0)
              {
                pc += 4;
                tmp = pc + branchTarget * 4;
                nextPC = tmp;
                goto OUTERContinue;
              }
              break;
            case 7: //BGTZ
              if (r [rs] > 0)
              {
                pc += 4;
                tmp = pc + branchTarget * 4;
                nextPC = tmp;
                goto OUTERContinue;
              }
              break;
            case 8: // ADDI
              r [rt] = r [rs] + signedImmediate;
              break;
            case 9: // ADDIU
              r [rt] = r [rs] + signedImmediate;
              break;
            case 10: // SLTI
              r [rt] = r [rs] < signedImmediate ? 1 : 0;
              break;
            case 11: // SLTIU
              r [rt] = (r [rs] & 0xffffffffL) < (signedImmediate & 0xffffffffL) ? 1 : 0;
              break;
            case 12: // ANDI
              r [rt] = r [rs] & unsignedImmediate;
              break;
            case 13: // ORI
              r [rt] = r [rs] | unsignedImmediate;
              break;
            case 14: // XORI
              r [rt] = r [rs] ^ unsignedImmediate;
              break;
            case 15: // LUI
              r [rt] = unsignedImmediate << 16;
              break;
            case 16:
              throw new ExecutionException("TLB/Exception support not implemented");
            case 17: // FPU
              throw new NotImplementedException("FPU Not implemented");
              /*
              {
                bool debug = false;
                string line = debug ? sourceLine(pc) : "";
                bool debugon = debug && (line.IndexOf("dtoa.c:51") >= 0 || line.IndexOf("dtoa.c:52") >= 0 || line.IndexOf("test.c") >= 0);
                if (rs > 8 && debugon)
                {
                  Console.WriteLine("               FP Op: " + op + "/" + rs + "/" + subcode + " " + line);
                }
                if (roundingMode() != 0 && rs != 6 && !((rs == 16 || rs == 17) && subcode == 36)) // CVT.W.Z - CTC.1
                {
                  throw new ExecutionException("Non-cvt.w.z operation attempted with roundingMode != round to nearest");
                }
                switch (rs)
                {
                  case 0: // MFC.1
                    r [rt] = f [rd];
                    break;
                  case 2: // CFC.1
                    if (fs != 31)
                    {
                      throw new ExecutionException("FCR " + fs + " unavailable");
                    }
                    r [rt] = fcsr;
                    break;
                  case 4: // MTC.1
                    f [rd] = r [rt];
                    break;
                  case 6: // CTC.1
                    if (fs != 31)
                    {
                      throw new ExecutionException("FCR " + fs + " unavailable");
                    }
                    fcsr = r [rt];
                    break;
                  case 8: // BC1F, BC1T
                    if (((fcsr & 0x800000) != 0) == ((((int)((uint)insn >> 16)) & 1) != 0))
                    {
                      pc += 4;
                      tmp = pc + branchTarget * 4;
                      nextPC = tmp;
                      goto OUTERContinue;
                    }
                    break;
                  case 16: // Single
                    {
                      switch (subcode)
                      {
                        case 0: // ADD.S
                          setFloat(fd, getFloat(fs) + getFloat(ft));
                          break;
                        case 1: // SUB.S
                          setFloat(fd, getFloat(fs) - getFloat(ft));
                          break;
                        case 2: // MUL.S
                          setFloat(fd, getFloat(fs) * getFloat(ft));
                          break;
                        case 3: // DIV.S
                          setFloat(fd, getFloat(fs) / getFloat(ft));
                          break;
                        case 5: // ABS.S
                          setFloat(fd, Math.Abs(getFloat(fs)));
                          break;
                        case 6: // MOV.S
                          f [fd] = f [fs];
                          break;
                        case 7: // NEG.S
                          setFloat(fd, -getFloat(fs));
                          break;
                        case 33: // CVT.D.S
                          setDouble(fd, getFloat(fs));
                          break;
                        case 36: // CVT.W.S
                          switch (roundingMode())
                          {
                            case 0: // Round to nearest
                              f [fd] = (int)Math.Floor(getFloat(fs) + 0.5f);
                              break;
                            case 1: // Round towards zero
                              f [fd] = (int)getFloat(fs);
                              break;
                            case 2: // Round towards plus infinity
                              f [fd] = (int)Math.Ceiling(getFloat(fs));
                              break;
                            case 3: // Round towards minus infinity
                              f [fd] = (int)Math.Floor(getFloat(fs));
                              break;
                          }
                          break;
                        case 50: // C.EQ.S
                          FC = getFloat(fs) == getFloat(ft);
                          break;
                        case 60: // C.LT.S
                          FC = getFloat(fs) < getFloat(ft);
                          break;
                        case 62: // C.LE.S
                          FC = getFloat(fs) <= getFloat(ft);
                          break;
                        default:
                          throw new ExecutionException("Invalid Instruction 17/" + rs + "/" + subcode + " at " + sourceLine(pc));
                      }
                      break;
                    }
                  case 17: // Double
                    {
                      switch (subcode)
                      {
                        case 0: // ADD.D
                          setDouble(fd, getDouble(fs) + getDouble(ft));
                          break;
                        case 1: // SUB.D
                          if (debugon)
                          {
                            Console.WriteLine("f" + fd + " = f" + fs + " (" + getDouble(fs) + ") - f" + ft + " (" + getDouble(ft) + ")");
                          }
                          setDouble(fd, getDouble(fs) - getDouble(ft));
                          break;
                        case 2: // MUL.D
                          if (debugon)
                          {
                            Console.WriteLine("f" + fd + " = f" + fs + " (" + getDouble(fs) + ") * f" + ft + " (" + getDouble(ft) + ")");
                          }
                          setDouble(fd, getDouble(fs) * getDouble(ft));
                          if (debugon)
                          {
                            Console.WriteLine("f" + fd + " = " + getDouble(fd));
                          }
                          break;
                        case 3: // DIV.D
                          setDouble(fd, getDouble(fs) / getDouble(ft));
                          break;
                        case 5: // ABS.D
                          setDouble(fd, Math.Abs(getDouble(fs)));
                          break;
                        case 6: // MOV.D
                          f [fd] = f [fs];
                          f [fd + 1] = f [fs + 1];
                          break;
                        case 7: // NEG.D
                          setDouble(fd, -getDouble(fs));
                          break;
                        case 32: // CVT.S.D
                          setFloat(fd, (float)getDouble(fs));
                          break;
                        case 36: // CVT.W.D
                          if (debugon)
                          {
                            Console.WriteLine("CVT.W.D rm: " + roundingMode() + " f" + fs + ":" + getDouble(fs));
                          }
                          switch (roundingMode())
                          {
                            case 0: // Round to nearest
                              f [fd] = (int)Math.Floor(getDouble(fs) + 0.5);
                              break;
                            case 1: // Round towards zero
                              f [fd] = (int)getDouble(fs);
                              break;
                            case 2: // Round towards plus infinity
                              f [fd] = (int)Math.Ceiling(getDouble(fs));
                              break;
                            case 3: // Round towards minus infinity
                              f [fd] = (int)Math.Floor(getDouble(fs));
                              break;
                          }
                          if (debugon)
                          {
                            Console.WriteLine("CVT.W.D: f" + fd + ":" + f [fd]);
                          }
                          break;
                        case 50: // C.EQ.D
                          FC = getDouble(fs) == getDouble(ft);
                          break;
                        case 60: // C.LT.D
                          FC = getDouble(fs) < getDouble(ft);
                          break;
                        case 62: // C.LE.D
                          FC = getDouble(fs) <= getDouble(ft);
                          break;
                        default:
                          throw new ExecutionException("Invalid Instruction 17/" + rs + "/" + subcode + " at " + sourceLine(pc));
                      }
                      break;
                    }
                  case 20: // Integer
                    {
                      switch (subcode)
                      {
                        case 32: // CVT.S.W
                          setFloat(fd, f [fs]);
                          break;
                        case 33: // CVT.D.W
                          setDouble(fd, f [fs]);
                          break;
                        default:
                          throw new ExecutionException("Invalid Instruction 17/" + rs + "/" + subcode + " at " + sourceLine(pc));
                      }
                      break;
                    }
                  default:
                    throw new ExecutionException("Invalid Instruction 17/" + rs);
                }
                break;
              }
              */
            case 18:
            case 19:
              throw new ExecutionException("No coprocessor installed");
            case 32: // LB
            {
              addr = r [rs] + signedImmediate;
              //try
              //{
              //  tmp = ReadPages(addr);
              //} catch (Exception e)
              //{
              //  tmp = memRead(addr & ~3);
              //}
              tmp = MemRead(addr);
              switch (addr & 3)
              {
                case 0:
                  tmp = ((int)((uint)tmp >> 24)) & 0xff;
                  break;
                case 1:
                  tmp = ((int)((uint)tmp >> 16)) & 0xff;
                  break;
                case 2:
                  tmp = ((int)((uint)tmp >> 8)) & 0xff;
                  break;
                case 3:
                  tmp = ((int)((uint)tmp >> 0)) & 0xff;
                  break;
              }
              if ((tmp & 0x80) != 0) // sign extend
              {
                tmp |= unchecked((int)0xffffff00);
              }
              r [rt] = tmp;
              break;
            }
            case 33: // LH
            {
              addr = r [rs] + signedImmediate;
//              try
//              {
//                tmp = ReadPages(addr);
//              } catch (Exception e)
//              {
//                tmp = memRead(addr & ~3);
//              }
              tmp = MemRead(addr);
              switch (addr & 3)
              {
                case 0:
                  tmp = ((int)((uint)tmp >> 16)) & 0xffff;
                  break;
                case 2:
                  tmp = ((int)((uint)tmp >> 0)) & 0xffff;
                  break;
                default:
                  throw new ReadFaultException(addr);
              }
              if ((tmp & 0x8000) != 0) // sign extend
              {
                tmp |= unchecked((int)0xffff0000);
              }
              r [rt] = tmp;
              break;
            }
            case 34: // LWL;
            {
              addr = r [rs] + signedImmediate;
//              try
//              {
//                tmp = readPages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)];
//              } catch (Exception e)
//              {
//                tmp = memRead(addr & ~3);
//              }
              tmp = MemRead(addr);

              switch (addr & 3)
              {
                case 0:
                  r [rt] = (r [rt] & 0x00000000) | (tmp << 0);
                  break;
                case 1:
                  r [rt] = (r [rt] & 0x000000ff) | (tmp << 8);
                  break;
                case 2:
                  r [rt] = (r [rt] & 0x0000ffff) | (tmp << 16);
                  break;
                case 3:
                  r [rt] = (r [rt] & 0x00ffffff) | (tmp << 24);
                  break;
              }
              break;
            }
            case 35: // LW
              addr = r [rs] + signedImmediate;
//              try
//              {
//                r [rt] = readPages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)];
//              } catch (Exception e)
//              {
//                r [rt] = memRead(addr);
//              }
              r[rt] = MemRead(addr);

              break;
            case 36: // LBU
            {
              addr = r [rs] + signedImmediate;
//              try
//              {
//                tmp = readPages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)];
//              } catch (Exception e)
//              {
//                tmp = memRead(addr);
//              }
              tmp = MemRead(addr);

              switch (addr & 3)
              {
                case 0:
                  r [rt] = ((int)((uint)tmp >> 24)) & 0xff;
                  break;
                case 1:
                  r [rt] = ((int)((uint)tmp >> 16)) & 0xff;
                  break;
                case 2:
                  r [rt] = ((int)((uint)tmp >> 8)) & 0xff;
                  break;
                case 3:
                  r [rt] = ((int)((uint)tmp >> 0)) & 0xff;
                  break;
              }
              break;
            }
            case 37: // LHU
            {
              addr = r [rs] + signedImmediate;
//              try
//              {
//                tmp = readPages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)];
//              } catch (Exception e)
//              {
//                tmp = memRead(addr & ~3);
//              }
              tmp = MemRead(addr);

              switch (addr & 3)
              {
                case 0:
                  r [rt] = ((int)((uint)tmp >> 16)) & 0xffff;
                  break;
                case 2:
                  r [rt] = ((int)((uint)tmp >> 0)) & 0xffff;
                  break;
                default:
                  throw new ReadFaultException(addr);
              }
              break;
            }
            case 38: // LWR
            {
              addr = r [rs] + signedImmediate;
//              try
//              {
//                tmp = readPages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)];
//              } catch (Exception e)
//              {
//                tmp = memRead(addr & ~3);
//              }
              tmp = MemRead(addr);

              switch (addr & 3)
              {
                case 0:
                  r [rt] = (r [rt] & unchecked((int)0xffffff00)) | ((int)((uint)tmp >> 24));
                  break;
                case 1:
                  r [rt] = (r [rt] & unchecked((int)0xffff0000)) | ((int)((uint)tmp >> 16));
                  break;
                case 2:
                  r [rt] = (r [rt] & unchecked((int)0xff000000)) | ((int)((uint)tmp >> 8));
                  break;
                case 3:
                  r [rt] = (r [rt] & 0x00000000) | ((int)((uint)tmp >> 0));
                  break;
              }
              break;
            }
            case 40: // SB
            {
              addr = r [rs] + signedImmediate;
//              try
//              {
//                tmp = readPages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)];
//              } catch (Exception e)
//              {
//                tmp = memRead(addr & ~3);
//              }
              tmp = MemRead(addr);

              switch (addr & 3)
              {
                case 0:
                  tmp = (tmp & 0x00ffffff) | ((r [rt] & 0xff) << 24);
                  break;
                case 1:
                  tmp = (tmp & unchecked((int)0xff00ffff)) | ((r [rt] & 0xff) << 16);
                  break;
                case 2:
                  tmp = (tmp & unchecked((int)0xffff00ff)) | ((r [rt] & 0xff) << 8);
                  break;
                case 3:
                  tmp = (tmp & unchecked((int)0xffffff00)) | ((r [rt] & 0xff) << 0);
                  break;
              }
//              try
//              {
//                writePages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)] = tmp;
//              } catch (Exception e)
//              {
//                memWrite(addr & ~3, tmp);
//              }
              MemWrite(addr,tmp);
              break;
            }
            case 41: // SH
            {
              addr = r [rs] + signedImmediate;
//              try
//              {
//                tmp = readPages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)];
//              } catch (Exception e)
//              {
//                tmp = memRead(addr & ~3);
//              }
              tmp = MemRead(addr);

              switch (addr & 3)
              {
                case 0:
                  tmp = (tmp & 0x0000ffff) | ((r [rt] & 0xffff) << 16);
                  break;
                case 2:
                  tmp = (tmp & unchecked((int)0xffff0000)) | ((r [rt] & 0xffff) << 0);
                  break;
                default:
                  throw new WriteFaultException(addr);
              }
//              try
//              {
//                writePages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)] = tmp;
//              } catch (Exception e)
//              {
//                memWrite(addr & ~3, tmp);
//              }
              MemWrite(addr,tmp);

              break;
            }
            case 42: // SWL
            {
              addr = r [rs] + signedImmediate;
              tmp = MemRead(addr & ~3);
              switch (addr & 3)
              {
                case 0:
                  tmp = (tmp & 0x00000000) | ((int)((uint)r [rt] >> 0));
                  break;
                case 1:
                  tmp = (tmp & unchecked((int)0xff000000)) | ((int)((uint)r [rt] >> 8));
                  break;
                case 2:
                  tmp = (tmp & unchecked((int)0xffff0000)) | ((int)((uint)r [rt] >> 16));
                  break;
                case 3:
                  tmp = (tmp & unchecked((int)0xffffff00)) | ((int)((uint)r [rt] >> 24));
                  break;
              }
//              try
//              {
//                writePages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)] = tmp;
//              } catch (Exception e)
//              {
//                memWrite(addr & ~3, tmp);
//              }
              MemWrite(addr,tmp);

              break;
            }
            case 43: // SW
              addr = r [rs] + signedImmediate;
//              try
//              {
//                writePages [(int)((uint)addr >> pageShift)] [((int)((uint)addr >> 2)) & (PAGE_WORDS - 1)] = r [rt];
//              } catch (Exception e)
//              {
//                memWrite(addr & ~3, r [rt]);
//              }
              MemWrite(addr,r[rt]);

              break;
            case 46: // SWR
            {
              addr = r [rs] + signedImmediate;
              tmp = MemRead(addr & ~3);
              switch (addr & 3)
              {
                case 0:
                  tmp = (tmp & 0x00ffffff) | (r [rt] << 24);
                  break;
                case 1:
                  tmp = (tmp & 0x0000ffff) | (r [rt] << 16);
                  break;
                case 2:
                  tmp = (tmp & 0x000000ff) | (r [rt] << 8);
                  break;
                case 3:
                  tmp = (tmp & 0x00000000) | (r [rt] << 0);
                  break;
              }
              MemWrite(addr & ~3, tmp);
              break;
            }
              // Needs to be atomic w/ threads
            case 48: // LWC0/LL
              r [rt] = MemRead(r [rs] + signedImmediate);
              break;
            case 49: // LWC1
              throw new NotImplementedException("No FPU");
              //f [rt] = memRead(r [rs] + signedImmediate);
              //break;
              // Needs to be atomic w/ threads
            case 56:
              MemWrite(r [rs] + signedImmediate, r [rt]);
              r [rt] = 1;
              break;
            case 57: // SWC1
              throw new NotImplementedException("No FPU");
              //memWrite(r [rs] + signedImmediate, f [rt]);
              //break;
            default:
              throw new ExecutionException("Invalid Instruction: " + op);
          }
          pc = nextPC;
          nextPC = pc + 4;
        OUTERContinue:
            ;
        }
      OUTERBreak:
          ; // for(;;)
      } catch (ExecutionException e)
      {
        this.pc = pc;
        throw e;
      }
      return 0;
    }


  }
}

