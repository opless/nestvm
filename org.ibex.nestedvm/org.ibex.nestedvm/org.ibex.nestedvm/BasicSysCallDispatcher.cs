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
  class BasicSysCallDispatcher : ISysCallDispatcher
  {


    #region constants    
    // these don't seem to match up to any particular operating system id!
    public const int SYS_null = 0;
    public const int SYS_exit = 1;
    public const int SYS_pause = 2;
    public const int SYS_open = 3;
    public const int SYS_close = 4;
    public const int SYS_read = 5;
    public const int SYS_write = 6;
    public const int SYS_sbrk = 7;
    public const int SYS_fstat = 8;
    public const int SYS_lseek = 10;
    public const int SYS_kill = 11;
    public const int SYS_getpid = 12;
    public const int SYS_call_internal = 13; // was calljava
    public const int SYS_stat = 14;
    public const int SYS_gettimeofday = 15;
    public const int SYS_sleep = 16;
    public const int SYS_times = 17;
    public const int SYS_mkdir = 18;
    public const int SYS_getpagesize = 19;
    public const int SYS_unlink = 20;
    public const int SYS_utime = 21;
    public const int SYS_chdir = 22;
    public const int SYS_pipe = 23;
    public const int SYS_dup2 = 24;
    public const int SYS_fork = 25;
    public const int SYS_waitpid = 26;
    public const int SYS_getcwd = 27;
    public const int SYS_exec = 28;
    public const int SYS_fcntl = 29;
    public const int SYS_rmdir = 30;
    public const int SYS_sysconf = 31;
    public const int SYS_readlink = 32;
    public const int SYS_lstat = 33;
    public const int SYS_symlink = 34;
    public const int SYS_link = 35;
    public const int SYS_getdents = 36;
    public const int SYS_memcpy = 37;
    public const int SYS_memset = 38;
    public const int SYS_dup = 39;
    public const int SYS_vfork = 40;
    public const int SYS_chroot = 41;
    public const int SYS_mknod = 42;
    public const int SYS_lchown = 43;
    public const int SYS_ftruncate = 44;
    public const int SYS_usleep = 45;
    public const int SYS_getppid = 46;
    public const int SYS_mkfifo = 47;
    public const int SYS_klogctl = 51;
    public const int SYS_realpath = 52;
    public const int SYS_sysctl = 53;
    public const int SYS_setpriority = 54;
    public const int SYS_getpriority = 55;
    public const int SYS_socket = 56;
    public const int SYS_connect = 57;
    public const int SYS_resolve_hostname = 58;
    public const int SYS_accept = 59;
    public const int SYS_setsockopt = 60;
    public const int SYS_getsockopt = 61;
    public const int SYS_listen = 62;
    public const int SYS_bind = 63;
    public const int SYS_shutdown = 64;
    public const int SYS_sendto = 65;
    public const int SYS_recvfrom = 66;
    public const int SYS_select = 67;
    public const int SYS_getuid = 68;
    public const int SYS_getgid = 69;
    public const int SYS_geteuid = 70;
    public const int SYS_getegid = 71;
    public const int SYS_getgroups = 72;
    public const int SYS_umask = 73;
    public const int SYS_chmod = 74;
    public const int SYS_fchmod = 75;
    public const int SYS_chown = 76;
    public const int SYS_fchown = 77;
    public const int SYS_access = 78;
    public const int SYS_alarm = 79;
    public const int SYS_setuid = 80;
    public const int SYS_setgid = 81;
    public const int SYS_send = 82;
    public const int SYS_recv = 83;
    public const int SYS_getsockname = 84;
    public const int SYS_getpeername = 85;
    public const int SYS_seteuid = 86;
    public const int SYS_setegid = 87;
    public const int SYS_setgroups = 88;
    public const int SYS_resolve_ip = 89;
    public const int SYS_setsid = 90;
    public const int SYS_fsync = 91;

    // added by SCW
    public const int SYS_mount = 92;
    public const int SYS_umount = 93;

    public const int AF_UNIX = 1;
    public const int AF_INET = 2;

    public const int SOCK_STREAM = 1;
    public const int SOCK_DGRAM = 2;

    public const int HOST_NOT_FOUND = 1;
    public const int TRY_AGAIN = 2;
    public const int NO_RECOVERY = 3;
    public const int NO_DATA = 4;

    public const int SOL_SOCKET = 0xffff;
    public const int SO_REUSEADDR = 0x0004;
    public const int SO_KEEPALIVE = 0x0008;
    public const int SO_BROADCAST = 0x0020;
    public const int SO_TYPE = 0x1008;

    public const int SHUT_RD = 0;
    public const int SHUT_WR = 1;
    public const int SHUT_RDWR = 2;

    public const int INADDR_ANY = 0;
    public const int INADDR_LOOPBACK = 0x7f000001;
    public const int INADDR_BROADCAST = unchecked((int)0xffffffff);

    public const int EPERM = 1;
    public const int ENOENT = 2;
    public const int ESRCH = 3;
    public const int EINTR = 4;
    public const int EIO = 5;
    public const int ENXIO = 6;
    public const int E2BIG = 7;
    public const int ENOEXEC = 8;
    public const int EBADF = 9;
    public const int ECHILD = 10;
    public const int EAGAIN = 11;
    public const int ENOMEM = 12;
    public const int EACCES = 13;
    public const int EFAULT = 14;
    public const int ENOTBLK = 15;
    public const int EBUSY = 16;
    public const int EEXIST = 17;
    public const int EXDEV = 18;
    public const int ENODEV = 19;
    public const int ENOTDIR = 20;
    public const int EISDIR = 21;
    public const int EINVAL = 22;
    public const int ENFILE = 23;
    public const int EMFILE = 24;
    public const int ENOTTY = 25;
    public const int ETXTBSY = 26;
    public const int EFBIG = 27;
    public const int ENOSPC = 28;
    public const int ESPIPE = 29;
    public const int EROFS = 30;
    public const int EMLINK = 31;
    public const int EPIPE = 32;
    public const int EDOM = 33;
    public const int ERANGE = 34;
    public const int ENOMSG = 35;
    public const int EIDRM = 36;
    public const int ECHRNG = 37;
    public const int EL2NSYNC = 38;
    public const int EL3HLT = 39;
    public const int EL3RST = 40;
    public const int ELNRNG = 41;
    public const int EUNATCH = 42;
    public const int ENOCSI = 43;
    public const int EL2HLT = 44;
    public const int EDEADLK = 45;
    public const int ENOLCK = 46;
    public const int EBADE = 50;
    public const int EBADR = 51;
    public const int EXFULL = 52;
    public const int ENOANO = 53;
    public const int EBADRQC = 54;
    public const int EBADSLT = 55;
    public const int EDEADLOCK = 56;
    public const int EBFONT = 57;
    public const int ENOSTR = 60;
    public const int ENODATA = 61;
    public const int ETIME = 62;
    public const int ENOSR = 63;
    public const int ENONET = 64;
    public const int ENOPKG = 65;
    public const int EREMOTE = 66;
    public const int ENOLINK = 67;
    public const int EADV = 68;
    public const int ESRMNT = 69;
    public const int ECOMM = 70;
    public const int EPROTO = 71;
    public const int EMULTIHOP = 74;
    public const int ELBIN = 75;
    public const int EDOTDOT = 76;
    public const int EBADMSG = 77;
    public const int EFTYPE = 79;
    public const int ENOTUNIQ = 80;
    public const int EBADFD = 81;
    public const int EREMCHG = 82;
    public const int ELIBACC = 83;
    public const int ELIBBAD = 84;
    public const int ELIBSCN = 85;
    public const int ELIBMAX = 86;
    public const int ELIBEXEC = 87;
    public const int ENOSYS = 88;
    public const int ENMFILE = 89;
    public const int ENOTEMPTY = 90;
    public const int ENAMETOOLONG = 91;
    public const int ELOOP = 92;
    public const int EOPNOTSUPP = 95;
    public const int EPFNOSUPPORT = 96;
    public const int ECONNRESET = 104;
    public const int ENOBUFS = 105;
    public const int EAFNOSUPPORT = 106;
    public const int EPROTOTYPE = 107;
    public const int ENOTSOCK = 108;
    public const int ENOPROTOOPT = 109;
    public const int ESHUTDOWN = 110;
    public const int ECONNREFUSED = 111;
    public const int EADDRINUSE = 112;
    public const int ECONNABORTED = 113;
    public const int ENETUNREACH = 114;
    public const int ENETDOWN = 115;
    public const int ETIMEDOUT = 116;
    public const int EHOSTDOWN = 117;
    public const int EHOSTUNREACH = 118;
    public const int EINPROGRESS = 119;
    public const int EALREADY = 120;
    public const int EDESTADDRREQ = 121;
    public const int EMSGSIZE = 122;
    public const int EPROTONOSUPPORT = 123;
    public const int ESOCKTNOSUPPORT = 124;
    public const int EADDRNOTAVAIL = 125;
    public const int ENETRESET = 126;
    public const int EISCONN = 127;
    public const int ENOTCONN = 128;
    public const int ETOOMANYREFS = 129;
    public const int EPROCLIM = 130;
    public const int EUSERS = 131;
    public const int EDQUOT = 132;
    public const int ESTALE = 133;
    public const int ENOTSUP = 134;
    public const int ENOMEDIUM = 135;
    public const int ENOSHARE = 136;
    public const int ECASECLASH = 137;
    public const int EILSEQ = 138;
    public const int EOVERFLOW = 139;

    public const int __ELASTERROR = 2000;

    public const int F_OK = 0;
    public const int R_OK = 4;
    public const int W_OK = 2;
    public const int X_OK = 1;

    public const int SEEK_SET = 0;
    public const int SEEK_CUR = 1;
    public const int SEEK_END = 2;

    public const int STDIN_FILENO = 0;
    public const int STDOUT_FILENO = 1;
    public const int STDERR_FILENO = 2;

    public const int _SC_ARG_MAX = 0;
    public const int _SC_CHILD_MAX = 1;
    public const int _SC_CLK_TCK = 2;
    public const int _SC_NGROUPS_MAX = 3;
    public const int _SC_OPEN_MAX = 4;
    public const int _SC_JOB_CONTROL = 5;
    public const int _SC_SAVED_IDS = 6;
    public const int _SC_VERSION = 7;
    public const int _SC_PAGESIZE = 8;
    public const int _SC_NPROCESSORS_CONF = 9;
    public const int _SC_NPROCESSORS_ONLN = 10;
    public const int _SC_PHYS_PAGES = 11;
    public const int _SC_AVPHYS_PAGES = 12;
    public const int _SC_MQ_OPEN_MAX = 13;
    public const int _SC_MQ_PRIO_MAX = 14;
    public const int _SC_RTSIG_MAX = 15;
    public const int _SC_SEM_NSEMS_MAX = 16;
    public const int _SC_SEM_VALUE_MAX = 17;
    public const int _SC_SIGQUEUE_MAX = 18;
    public const int _SC_TIMER_MAX = 19;
    public const int _SC_TZNAME_MAX = 20;
    public const int _SC_ASYNCHRONOUS_IO = 21;
    public const int _SC_FSYNC = 22;
    public const int _SC_MAPPED_FILES = 23;
    public const int _SC_MEMLOCK = 24;
    public const int _SC_MEMLOCK_RANGE = 25;
    public const int _SC_MEMORY_PROTECTION = 26;
    public const int _SC_MESSAGE_PASSING = 27;
    public const int _SC_PRIORITIZED_IO = 28;
    public const int _SC_REALTIME_SIGNALS = 29;
    public const int _SC_SEMAPHORES = 30;
    public const int _SC_SHARED_MEMORY_OBJECTS = 31;
    public const int _SC_SYNCHRONIZED_IO = 32;
    public const int _SC_TIMERS = 33;
    public const int _SC_AIO_LISTIO_MAX = 34;
    public const int _SC_AIO_MAX = 35;
    public const int _SC_AIO_PRIO_DELTA_MAX = 36;
    public const int _SC_DELAYTIMER_MAX = 37;
    public const int _SC_THREAD_KEYS_MAX = 38;
    public const int _SC_THREAD_STACK_MIN = 39;
    public const int _SC_THREAD_THREADS_MAX = 40;
    public const int _SC_TTY_NAME_MAX = 41;
    public const int _SC_THREADS = 42;
    public const int _SC_THREAD_ATTR_STACKADDR = 43;
    public const int _SC_THREAD_ATTR_STACKSIZE = 44;
    public const int _SC_THREAD_PRIORITY_SCHEDULING = 45;
    public const int _SC_THREAD_PRIO_INHERIT = 46;
    public const int _SC_THREAD_PRIO_PROTECT = 47;
    public const int _SC_THREAD_PROCESS_SHARED = 48;
    public const int _SC_THREAD_SAFE_FUNCTIONS = 49;
    public const int _SC_GETGR_R_SIZE_MAX = 50;
    public const int _SC_GETPW_R_SIZE_MAX = 51;
    public const int _SC_LOGIN_NAME_MAX = 52;
    public const int _SC_THREAD_DESTRUCTOR_ITERATIONS = 53;
    public const int _SC_STREAM_MAX = 100;
    public const int _SC_PRIORITY_SCHEDULING = 101;

    public const int _PC_LINK_MAX = 0;
    public const int _PC_MAX_CANON = 1;
    public const int _PC_MAX_INPUT = 2;
    public const int _PC_NAME_MAX = 3;
    public const int _PC_PATH_MAX = 4;
    public const int _PC_PIPE_BUF = 5;
    public const int _PC_CHOWN_RESTRICTED = 6;
    public const int _PC_NO_TRUNC = 7;
    public const int _PC_VDISABLE = 8;
    public const int _PC_ASYNC_IO = 9;
    public const int _PC_PRIO_IO = 10;
    public const int _PC_SYNC_IO = 11;
    public const int _PC_POSIX_PERMISSIONS = 90;
    public const int _PC_POSIX_SECURITY = 91;

    public const int MAXPATHLEN = 1024;

    public const int ARG_MAX = 65536;
    public const int CHILD_MAX = 40;
    public const int LINK_MAX = 32767;
    public const int MAX_CANON = 255;
    public const int MAX_INPUT = 255;
    public const int NAME_MAX = 255;
    public const int NGROUPS_MAX = 16;
    public const int OPEN_MAX = 64;
    public const int PATH_MAX = 1024;
    public const int PIPE_BUF = 512;
    public const int IOV_MAX = 1024;

    public const int BC_BASE_MAX = 99;
    public const int BC_DIM_MAX = 2048;
    public const int BC_SCALE_MAX = 99;
    public const int BC_STRING_MAX = 1000;

    public const int COLL_WEIGHTS_MAX = 0;
    public const int EXPR_NEST_MAX = 32;
    public const int LINE_MAX = 2048;
    public const int RE_DUP_MAX = 255;

    public const int CTL_MAXNAME = 12;
    public const int CTL_UNSPEC = 0;
    public const int CTL_KERN = 1;
    public const int CTL_VM = 2;
    public const int CTL_VFS = 3;
    public const int CTL_NET = 4;
    public const int CTL_DEBUG = 5;
    public const int CTL_HW = 6;
    public const int CTL_MACHDEP = 7;
    public const int CTL_USER = 8;
    public const int CTL_P1003_1B = 9;
    public const int CTL_MAXID = 10;

    public const int KERN_OSTYPE = 1;
    public const int KERN_OSRELEASE = 2;
    public const int KERN_OSREV = 3;
    public const int KERN_VERSION = 4;
    public const int KERN_MAXVNODES = 5;
    public const int KERN_MAXPROC = 6;
    public const int KERN_MAXFILES = 7;
    public const int KERN_ARGMAX = 8;
    public const int KERN_SECURELVL = 9;
    public const int KERN_HOSTNAME = 10;
    public const int KERN_HOSTID = 11;
    public const int KERN_CLOCKRATE = 12;
    public const int KERN_VNODE = 13;
    public const int KERN_PROC = 14;
    public const int KERN_FILE = 15;
    public const int KERN_PROF = 16;
    public const int KERN_POSIX1 = 17;
    public const int KERN_NGROUPS = 18;
    public const int KERN_JOB_CONTROL = 19;
    public const int KERN_SAVED_IDS = 20;
    public const int KERN_BOOTTIME = 21;
    public const int KERN_NISDOMAINNAME = 22;
    public const int KERN_UPDATEINTERVAL = 23;
    public const int KERN_OSRELDATE = 24;
    public const int KERN_NTP_PLL = 25;
    public const int KERN_BOOTFILE = 26;
    public const int KERN_MAXFILESPERPROC = 27;
    public const int KERN_MAXPROCPERUID = 28;
    public const int KERN_DUMPDEV = 29;
    public const int KERN_IPC = 30;
    public const int KERN_DUMMY = 31;
    public const int KERN_PS_STRINGS = 32;
    public const int KERN_USRSTACK = 33;
    public const int KERN_LOGSIGEXIT = 34;
    public const int KERN_MAXID = 35;
    public const int KERN_PROC_ALL = 0;
    public const int KERN_PROC_PID = 1;
    public const int KERN_PROC_PGRP = 2;
    public const int KERN_PROC_SESSION = 3;
    public const int KERN_PROC_TTY = 4;
    public const int KERN_PROC_UID = 5;
    public const int KERN_PROC_RUID = 6;
    public const int KERN_PROC_ARGS = 7;

    public const int KIPC_MAXSOCKBUF = 1;
    public const int KIPC_SOCKBUF_WASTE = 2;
    public const int KIPC_SOMAXCONN = 3;
    public const int KIPC_MAX_LINKHDR = 4;
    public const int KIPC_MAX_PROTOHDR = 5;
    public const int KIPC_MAX_HDR = 6;
    public const int KIPC_MAX_DATALEN = 7;
    public const int KIPC_MBSTAT = 8;
    public const int KIPC_NMBCLUSTERS = 9;

    public const int HW_MACHINE = 1;
    public const int HW_MODEL = 2;
    public const int HW_NCPU = 3;
    public const int HW_BYTEORDER = 4;
    public const int HW_PHYSMEM = 5;
    public const int HW_USERMEM = 6;
    public const int HW_PAGESIZE = 7;
    public const int HW_DISKNAMES = 8;
    public const int HW_DISKSTATS = 9;
    public const int HW_FLOATINGPT = 10;
    public const int HW_MACHINE_ARCH = 11;
    public const int HW_MAXID = 12;

    public const int USER_CS_PATH = 1;
    public const int USER_BC_BASE_MAX = 2;
    public const int USER_BC_DIM_MAX = 3;
    public const int USER_BC_SCALE_MAX = 4;
    public const int USER_BC_STRING_MAX = 5;
    public const int USER_COLL_WEIGHTS_MAX = 6;
    public const int USER_EXPR_NEST_MAX = 7;
    public const int USER_LINE_MAX = 8;
    public const int USER_RE_DUP_MAX = 9;
    public const int USER_POSIX2_VERSION = 10;
    public const int USER_POSIX2_C_BIND = 11;
    public const int USER_POSIX2_C_DEV = 12;
    public const int USER_POSIX2_CHAR_TERM = 13;
    public const int USER_POSIX2_FORT_DEV = 14;
    public const int USER_POSIX2_FORT_RUN = 15;
    public const int USER_POSIX2_LOCALEDEF = 16;
    public const int USER_POSIX2_SW_DEV = 17;
    public const int USER_POSIX2_UPE = 18;
    public const int USER_STREAM_MAX = 19;
    public const int USER_TZNAME_MAX = 20;
    public const int USER_MAXID = 21;

    public const int CTL_P1003_1B_ASYNCHRONOUS_IO = 1;
    public const int CTL_P1003_1B_MAPPED_FILES = 2;
    public const int CTL_P1003_1B_MEMLOCK = 3;
    public const int CTL_P1003_1B_MEMLOCK_RANGE = 4;
    public const int CTL_P1003_1B_MEMORY_PROTECTION = 5;
    public const int CTL_P1003_1B_MESSAGE_PASSING = 6;
    public const int CTL_P1003_1B_PRIORITIZED_IO = 7;
    public const int CTL_P1003_1B_PRIORITY_SCHEDULING = 8;
    public const int CTL_P1003_1B_REALTIME_SIGNALS = 9;
    public const int CTL_P1003_1B_SEMAPHORES = 10;
    public const int CTL_P1003_1B_FSYNC = 11;
    public const int CTL_P1003_1B_SHARED_MEMORY_OBJECTS = 12;
    public const int CTL_P1003_1B_SYNCHRONIZED_IO = 13;
    public const int CTL_P1003_1B_TIMERS = 14;
    public const int CTL_P1003_1B_AIO_LISTIO_MAX = 15;
    public const int CTL_P1003_1B_AIO_MAX = 16;
    public const int CTL_P1003_1B_AIO_PRIO_DELTA_MAX = 17;
    public const int CTL_P1003_1B_DELAYTIMER_MAX = 18;
    public const int CTL_P1003_1B_MQ_OPEN_MAX = 19;
    public const int CTL_P1003_1B_PAGESIZE = 20;
    public const int CTL_P1003_1B_RTSIG_MAX = 21;
    public const int CTL_P1003_1B_SEM_NSEMS_MAX = 22;
    public const int CTL_P1003_1B_SEM_VALUE_MAX = 23;
    public const int CTL_P1003_1B_SIGQUEUE_MAX = 24;
    public const int CTL_P1003_1B_TIMER_MAX = 25;
    public const int CTL_P1003_1B_MAXID = 26;

    public const int F_UNLKSYS = 4;
    public const int F_CNVT = 12;
    public const int F_SETFD = 2;
    public const int F_SETFL = 4;
    public const int F_SETLK = 8;
    public const int F_SETOWN = 6;
    public const int F_RDLCK = 1;
    public const int F_WRLCK = 2;
    public const int F_SETLKW = 9;
    public const int F_GETFD = 1;
    public const int F_DUPFD = 0;
    public const int O_WRONLY = 1;
    public const int F_RSETLKW = 13;
    public const int O_RDWR = 2;
    public const int F_RGETLK = 10;
    public const int O_RDONLY = 0;
    public const int F_UNLCK = 3;
    public const int F_GETOWN = 5;
    public const int F_RSETLK = 11;
    public const int F_GETFL = 3;
    public const int F_GETLK = 7;

    #endregion




    int sys_exit(ICpuInterpreter interp,int value)
    {
      return interp.GetProcMgr().Exit(interp,value);
    }

    int sys_pause(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    int sys_write(ICpuInterpreter interp,int fd, int addr, int count)
    {

      return interp.GetVirtFS().Write(interp,fd,addr,count);
    }

    int sys_fstat(ICpuInterpreter interp,int fd, int buffAddr)
    {
      return interp.GetVirtFS().FStat(interp,fd,buffAddr);
    }

    int sbrk(ICpuInterpreter interp,int a)
    {
      throw new NotImplementedException();
    }

    int sys_open(ICpuInterpreter interp,int nameAddr, int oflag, int mode)
    {
      return interp.GetVirtFS().Open(interp,nameAddr,oflag,mode);
    }

    int sys_close(ICpuInterpreter interp,int fd)
    {
      return interp.GetVirtFS().Close(interp, fd);
    }

    int sys_read(ICpuInterpreter interp,int fd, int addr, int count)
    {
      return interp.GetVirtFS().Read(interp, fd, addr, count);
    }

    int sys_lseek(ICpuInterpreter interp,int fd, int offset, int whence)
    {
      return interp.GetVirtFS().LSeek(interp,fd,offset,whence);
    }

    int sys_ftruncate(ICpuInterpreter interp,int fd, int length)
    {
      return interp.GetVirtFS().FTruncate(interp,fd,length);
    }

    int sys_getpid(ICpuInterpreter interp)
    {
      return interp.GetProcMgr().GetProcessId(interp);
    }

    int sys_call_internal(ICpuInterpreter interp,int a, int b, int c, int d)
    {
      throw new NotImplementedException();
    }

    int sys_gettimeofday(ICpuInterpreter interp,int a, int b)
    {
      throw new NotImplementedException();
    }

    int sys_sleep(ICpuInterpreter interp,int a)
    {
      throw new NotImplementedException();
    }

    int sys_times(ICpuInterpreter interp,int a)
    {
      throw new NotImplementedException();
    }

    int sys_getpagesize(ICpuInterpreter interp)
    {
      throw new NotImplementedException();
    }

    int sys_fcntl(ICpuInterpreter interp,int fd, int cmd, int arg)
    {
      return interp.GetVirtFS().FCntl(interp,fd,cmd,arg);
    }

    int sys_sysconf(ICpuInterpreter interp,int a)
    {
      throw new NotImplementedException();
    }

    int sys_getuid(ICpuInterpreter interp)
    {
      return interp.GetProcMgr().GetUserId(interp);
    }

    int sys_geteuid(ICpuInterpreter interp)
    {
      return interp.GetProcMgr().GetEffectiveUserId(interp);
    }

    int sys_getgid(ICpuInterpreter interp)
    {
      return interp.GetProcMgr().GetGroupId(interp);
    }

    int sys_getegid(ICpuInterpreter interp)
    {
      return interp.GetProcMgr().GetEffectiveGroupId(interp);
    }

    int fsync(ICpuInterpreter interp,int fd)
    {
      return interp.GetVirtFS().FSync(interp,fd);
    }

    int sys_kill(ICpuInterpreter interp,int pid, int signal)
    {
      return interp.GetProcMgr().Kill(interp,pid,signal);
    }

    int sys_fork(ICpuInterpreter interp)
    {
      return interp.GetProcMgr().Fork(interp);
    }

    int sys_pipe(ICpuInterpreter interp,int addr)
    {
      return interp.GetVirtFS().Pipe(interp,addr);
    }

    int sys_dup2(ICpuInterpreter interp,int fda, int fdb)
    {
      return interp.GetVirtFS().Dup2(interp,fda,fdb);
    }

    int sys_dup(ICpuInterpreter interp,int fd)
    {
      return interp.GetVirtFS().Dup(interp,fd);
    }

    int sys_waitpid(int a, int b, int c)
    {
      throw new NotImplementedException();
    }

    int sys_stat(ICpuInterpreter interp,int cstringArg, int addr)
    {
      return interp.GetVirtFS().Stat(interp,cstringArg,addr);
    }

    int sys_lstat(ICpuInterpreter interp,int cstringArg, int addr)
    {
      return interp.GetVirtFS().LStat(interp,cstringArg,addr);
    }

    int sys_mkdir(ICpuInterpreter interp,int cstringArg, int mode)
    {
      return interp.GetVirtFS().MkDir(interp,cstringArg,mode);
    }

    int sys_getcwd(ICpuInterpreter interp,int addr, int size)
    {
      //return interp.GetVirtFS().GetCwd(interp,addr,size);
      return interp.GetProcMgr().GetCwd(interp,addr,size);
    }

    int sys_exec(int a, int b, int c)
    {
      throw new NotImplementedException();
    }

    int sys_chdir(ICpuInterpreter interp,int cstringArg)
    {
      return interp.GetProcMgr().ChDir(interp, cstringArg);
    }

    int sys_getdents(ICpuInterpreter interp,int a, int b, int c, int d)
    {
      throw new NotImplementedException();
    }

    int sys_getppid(ICpuInterpreter interp)
    {
      return interp.GetProcMgr().GetPPid(interp);
    }

    int sys_unlink(ICpuInterpreter interp,int cstringArg)
    {
      return interp.GetVirtFS().Unlink(interp,cstringArg);
    }

    int sys_sysctl(ICpuInterpreter interp,int nameAddr, int nameLen, int oldP, int oldLenAddr, int newP, int newLen)//(int a, int b, int c, int d, int e, int f)
    {
      return interp.GetProcMgr().SysCtl(interp,nameAddr,nameLen,oldP,oldLenAddr,newP,newLen);
    }

    int sys_access(ICpuInterpreter interp,int cstringArg, int mode)
    {
      return interp.GetVirtFS().Access(interp,cstringArg,mode);
    }

    int sys_realpath(ICpuInterpreter interp,int inAddr, int outAddr)
    {
      return interp.GetVirtFS().RealPath(interp,inAddr,outAddr);
    }

    int sys_chown(ICpuInterpreter interp,int cStringAddr, int owner, int group)
    {
      return interp.GetVirtFS().Chown(interp,cStringAddr,owner,group);
    }
    
    int sys_lchown(ICpuInterpreter interp,int cStringAddr, int owner, int group)
    {
      return interp.GetVirtFS().LChown(interp,cStringAddr,owner,group); // doesn't follow symlinks
    }
    
    int sys_fchown(ICpuInterpreter interp,int fd, int owner, int group)
    {
      return interp.GetVirtFS().FChown(interp,fd,owner,group); 
    }

    int sys_chmod(ICpuInterpreter interp,int cStringAddr, int mode)
    {
      return interp.GetVirtFS().Chmod(interp,cStringAddr,mode); 
    }

    int sys_fchmod(ICpuInterpreter interp,int fd, int mode)
    {
      return interp.GetVirtFS().FChmod(interp,fd,mode); 
    }

    int sys_fcntl_lock(ICpuInterpreter interp,int fd, int cmd, int arg)
    {
      return interp.GetVirtFS().FCntlLock(interp,fd,cmd,arg);
    }

    int sys_umask(ICpuInterpreter interp,int mode)
    {
      return interp.GetProcMgr().UMask(mode);
    }

    int sys_mount(ICpuInterpreter interp, int cStringType, int cStringDir, int flags, int d, int e)
    {
      throw new NotImplementedException();
    }

    int sys_unmount(ICpuInterpreter interp, int cStringAddr, int flags)
    {
      throw new NotImplementedException();
    }

    public int Dispatch(ICpuInterpreter interp,int syscall, int a, int b, int c, int d, int e, int f)
    {
      switch (syscall)
      {
        case SYS_null:
          return 0;
        case SYS_exit:
            return sys_exit(interp,a);
        case SYS_pause:
            return sys_pause(interp);
        case SYS_write:
            return sys_write(interp,a, b, c);
        case SYS_fstat:
            return sys_fstat(interp,a, b);
        case SYS_sbrk:
            return sbrk(interp,a);
        case SYS_open:
            return sys_open(interp,a, b, c);
        case SYS_close:
            return sys_close(interp,a);
        case SYS_read:
            return sys_read(interp,a, b, c);
        case SYS_lseek:
            return sys_lseek(interp,a, b, c);
        case SYS_ftruncate:
            return sys_ftruncate(interp,a, b);
        case SYS_getpid:
            return sys_getpid(interp);
        case SYS_call_internal:
            return sys_call_internal(interp,a, b, c, d);
        case SYS_gettimeofday:
            return sys_gettimeofday(interp,a, b);
        case SYS_sleep:
            return sys_sleep(interp,a);
        case SYS_times:
            return sys_times(interp,a);
        case SYS_getpagesize:
            return sys_getpagesize(interp);
        case SYS_fcntl:
            return sys_fcntl(interp,a, b, c);
        case SYS_sysconf:
            return sys_sysconf(interp,a);
        case SYS_getuid:
            return sys_getuid(interp);
        case SYS_geteuid:
            return sys_geteuid(interp);
        case SYS_getgid:
            return sys_getgid(interp);
        case SYS_getegid:
            return sys_getegid(interp);
          
        case SYS_fsync:
            return fsync(interp,a);
        case SYS_memcpy:
          interp.GetVirtMem().MemCpy(a, b, c);
          return a;
        case SYS_memset:
          interp.GetVirtMem().MemSet(a, b, c);
          return a;
          
        case SYS_kill:
          return sys_kill(interp,a, b);
        case SYS_fork:
          return sys_fork(interp);
        case SYS_pipe:
          return sys_pipe(interp,a);
        case SYS_dup2:
          return sys_dup2(interp,a, b);
        case SYS_dup:
          return sys_dup(interp,a);
        case SYS_waitpid:
          return sys_waitpid(interp,a, b, c);
        case SYS_stat:
          return sys_stat(interp,a, b);
        case SYS_lstat:
          return sys_lstat(interp,a, b);
        case SYS_mkdir:
          return sys_mkdir(interp,a, b);
        case SYS_getcwd:
          return sys_getcwd(interp,a, b);
        case SYS_chdir:
          return sys_chdir(interp,a);
        case SYS_exec:
          return sys_exec(interp,a, b, c);
        case SYS_getdents:
          return sys_getdents(interp,a, b, c, d);
        case SYS_unlink:
          return sys_unlink(interp,a);
        case SYS_getppid:
          return sys_getppid(interp);
          /*
        case SYS_socket:
          return sys_socket(a, b, c);
        case SYS_connect:
          return sys_connect(a, b, c);
        case SYS_resolve_hostname:
          return sys_resolve_hostname(a, b, c);
        case SYS_setsockopt:
          return sys_setsockopt(a, b, c, d, e);
        case SYS_getsockopt:
          return sys_getsockopt(a, b, c, d, e);
        case SYS_bind:
          return sys_bind(a, b, c);
        case SYS_listen:
          return sys_listen(a, b);
        case SYS_accept:
          return sys_accept(a, b, c);
        case SYS_shutdown:
          return sys_shutdown(a, b);
          */
        case SYS_sysctl:
          return sys_sysctl(interp,a, b, c, d, e, f);
          /*
        case SYS_sendto:
          return sys_sendto(a, b, c, d, e, f);
        case SYS_recvfrom:
          return sys_recvfrom(a, b, c, d, e, f);
        case SYS_select:
          return sys_select(a, b, c, d, e);
          */
        case SYS_access:
          return sys_access(interp,a, b);
        case SYS_realpath:
          return sys_realpath(interp,a, b);
        case SYS_chown:
          return sys_chown(interp,a, b, c);
        case SYS_lchown:
          return sys_lchown(interp,a, b, c);
        case SYS_fchown:
          return sys_fchown(interp,a, b, c);
        case SYS_chmod:
          return sys_chmod(interp,a, b, c);
        case SYS_fchmod:
          return sys_fchmod(interp,a, b, c);
        case SYS_fcntl:
          return sys_fcntl_lock(interp,a, b, c);
        case SYS_umask:
          return sys_umask(interp,a);

          // scw
        case SYS_mount:
          return sys_mount(interp,a,b,c,d,e);
        case SYS_umount:
          return sys_unmount(interp,a);
        
        default:
          //if (STDERR_DIAG)
          //{
            Console.Error.WriteLine("Attempted to use unknown syscall: " + syscall);
          //}
          return -ENOSYS;
      }
    
    }
  }

}

