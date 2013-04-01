using System;
using System.Text;

// Copyright 2000-2005 the Contributors, as shown in the revision logs.
// Licensed under the Apache License 2.0 ("the License").
// You may not use this file except in compliance with the License.
using System.IO;

namespace org.ibex.nestedvm.util
{

	public class ELF
	{
		private const int ELF_MAGIC = 0x7f454c46; // '\177', 'E', 'L', 'F'

		public const int ELFCLASSNONE = 0;
		public const int ELFCLASS32 = 1;
		public const int ELFCLASS64 = 2;

		public const int ELFDATANONE = 0;
		public const int ELFDATA2LSB = 1;
		public const int ELFDATA2MSB = 2;

		public const int SHT_SYMTAB = 2;
		public const int SHT_STRTAB = 3;
		public const int SHT_NOBITS = 8;

		public const int SHF_WRITE = 1;
		public const int SHF_ALLOC = 2;
		public const int SHF_EXECINSTR = 4;

		public const int PF_X = 0x1;
		public const int PF_W = 0x2;
		public const int PF_R = 0x4;

		public const int PT_LOAD = 1;

		public const short ET_EXEC = 2;
		public const short EM_MIPS = 8;


		private Seekable data;

		public ELFIdent ident;
		public ELFHeader header;
		public PHeader[] pheaders;
		public SHeader[] sheaders;

		private sbyte[] stringTable;

		private bool sectionReaderActive;


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readFully(byte[] buf) throws IOException
		private void readFully(sbyte[] buf)
		{
			int len = buf.Length;
			int pos = 0;
			while (len > 0)
			{
				int n = data.read(buf,pos,len);
				if (n == -1)
				{
					throw new IOException("EOF");
				}
				pos += n;
				len -= n;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int readIntBE() throws IOException
		private int readIntBE()
		{
			sbyte[] buf = new sbyte[4];
			readFully(buf);
			return ((buf[0] & 0xff) << 24) | ((buf[1] & 0xff) << 16) | ((buf[2] & 0xff) << 8) | ((buf[3] & 0xff) << 0);
		}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int readInt() throws IOException
		private int readInt()
		{
			int x = readIntBE();
			if (ident != null && ident.data == ELFDATA2LSB)
			{
				x = ((x << 24) & unchecked((int)0xff000000)) | ((x << 8) & 0xff0000) | (((int)((uint)x>>8)) & 0xff00) | ((x>>24) & 0xff);
			}
			return x;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private short readShortBE() throws IOException
		private short readShortBE()
		{
			sbyte[] buf = new sbyte[2];
			readFully(buf);
			return (short)(((buf[0] & 0xff) << 8) | ((buf[1] & 0xff) << 0));
		}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private short readShort() throws IOException
		private short readShort()
		{
			short x = readShortBE();
			if (ident != null && ident.data == ELFDATA2LSB)
			{
				x = unchecked((short)((((x << 8) & 0xff00) | ((x>>8) & 0xff)) & 0xffff));
			}
			return x;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private byte readByte() throws IOException
		private sbyte readByte()
		{
			sbyte[] buf = new sbyte[1];
			readFully(buf);
			return buf[0];
		}

		public class ELFIdent
		{
			private readonly ELF outerInstance;

			public sbyte klass;
			public sbyte data;
			public sbyte osabi;
			public sbyte abiversion;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: ELFIdent() throws IOException
			internal ELFIdent(ELF outerInstance)
			{
				this.outerInstance = outerInstance;
				if (outerInstance.readIntBE() != ELF_MAGIC)
				{
					throw new ELFException(outerInstance, "Bad Magic");
				}

				klass = outerInstance.readByte();
				if (klass != ELFCLASS32)
				{
					throw new ELFException(outerInstance, "org.ibex.nestedvm.util.ELF does not suport 64-bit binaries");
				}

				data = outerInstance.readByte();
				if (data != ELFDATA2LSB && data != ELFDATA2MSB)
				{
					throw new ELFException(outerInstance, "Unknown byte order");
				}

				outerInstance.readByte(); // version
				osabi = outerInstance.readByte();
				abiversion = outerInstance.readByte();
				for (int i = 0;i < 7;i++) // padding
				{
					outerInstance.readByte();
				}
			}
		}

		public class ELFHeader
		{
			private readonly ELF outerInstance;

			public short type;
			public short machine;
			public int version;
			public int entry;
			public int phoff;
			public int shoff;
			public int flags;
			public short ehsize;
			public short phentsize;
			public short phnum;
			public short shentsize;
			public short shnum;
			public short shstrndx;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: ELFHeader() throws IOException
			internal ELFHeader(ELF outerInstance)
			{
				this.outerInstance = outerInstance;
				type = outerInstance.readShort();
				machine = outerInstance.readShort();
				version = outerInstance.readInt();
				if (version != 1)
				{
					throw new ELFException(outerInstance, "version != 1");
				}
				entry = outerInstance.readInt();
				phoff = outerInstance.readInt();
				shoff = outerInstance.readInt();
				flags = outerInstance.readInt();
				ehsize = outerInstance.readShort();
				phentsize = outerInstance.readShort();
				phnum = outerInstance.readShort();
				shentsize = outerInstance.readShort();
				shnum = outerInstance.readShort();
				shstrndx = outerInstance.readShort();
			}
		}

		public class PHeader
		{
			private readonly ELF outerInstance;

			public int type;
			public int offset;
			public int vaddr;
			public int paddr;
			public int filesz;
			public int memsz;
			public int flags;
			public int align;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: PHeader() throws IOException
			internal PHeader(ELF outerInstance)
			{
				this.outerInstance = outerInstance;
				type = outerInstance.readInt();
				offset = outerInstance.readInt();
				vaddr = outerInstance.readInt();
				paddr = outerInstance.readInt();
				filesz = outerInstance.readInt();
				memsz = outerInstance.readInt();
				flags = outerInstance.readInt();
				align = outerInstance.readInt();
				if (filesz > memsz)
				{
					throw new ELFException(outerInstance, "ELF inconsistency: filesz > memsz (" + toHex(filesz) + " > " + toHex(memsz) + ")");
				}
			}

			public virtual bool writable()
			{
				return (flags & PF_W) != 0;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public InputStream getInputStream() throws IOException
			public virtual InputStream InputStream
			{
				get
				{
					return new InputStream(new SectionInputStream(outerInstance, offset,offset + filesz));
				}
			}
		}

		public class SHeader
		{
			private readonly ELF outerInstance;

			internal int nameidx;
			public string name;
			public int type;
			public int flags;
			public int addr;
			public int offset;
			public int size;
			public int link;
			public int info;
			public int addralign;
			public int entsize;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: SHeader() throws IOException
			internal SHeader(ELF outerInstance)
			{
				this.outerInstance = outerInstance;
				nameidx = outerInstance.readInt();
				type = outerInstance.readInt();
				flags = outerInstance.readInt();
				addr = outerInstance.readInt();
				offset = outerInstance.readInt();
				size = outerInstance.readInt();
				link = outerInstance.readInt();
				info = outerInstance.readInt();
				addralign = outerInstance.readInt();
				entsize = outerInstance.readInt();
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public InputStream getInputStream() throws IOException
			public virtual InputStream InputStream
			{
				get
				{
					return new InputStream(new SectionInputStream(outerInstance, offset, type == SHT_NOBITS ? 0 : offset + size));
				}
			}

			public virtual bool Text
			{
				get
				{
					return name.Equals(".text");
				}
			}
			public virtual bool Data
			{
				get
				{
					return name.Equals(".data") || name.Equals(".sdata") || name.Equals(".rodata") || name.Equals(".ctors") || name.Equals(".dtors");
				}
			}
			public virtual bool BSS
			{
				get
				{
					return name.Equals(".bss") || name.Equals(".sbss");
				}
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public ELF(String file) throws IOException, ELFException
		public ELF(string file) : this(new File(file,false))
		{
		}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public ELF(Seekable data) throws IOException, ELFException
		public ELF(Seekable data)
		{
			this.data = data;
			ident = new ELFIdent(this);
			header = new ELFHeader(this);
			pheaders = new PHeader[header.phnum];
			for (int i = 0;i < header.phnum;i++)
			{
				data.seek(header.phoff + i * header.phentsize);
				pheaders[i] = new PHeader(this);
			}
			sheaders = new SHeader[header.shnum];
			for (int i = 0;i < header.shnum;i++)
			{
				data.seek(header.shoff + i * header.shentsize);
				sheaders[i] = new SHeader(this);
			}
			if (header.shstrndx < 0 || header.shstrndx >= header.shnum)
			{
				throw new ELFException(this, "Bad shstrndx");
			}
			data.seek(sheaders[header.shstrndx].offset);
			stringTable = new sbyte[sheaders[header.shstrndx].size];
			readFully(stringTable);

			for (int i = 0;i < header.shnum;i++)
			{
				SHeader s = sheaders[i];
				s.name = getString(s.nameidx);
			}
		}

		private string getString(int off)
		{
			return getString(off,stringTable);
		}
		private string getString(int off, sbyte[] strtab)
		{
			StringBuilder sb = new StringBuilder();
			if (off < 0 || off >= strtab.Length)
			{
				return "<invalid strtab entry>";
			}
			while (off >= 0 && off < strtab.Length && strtab[off] != 0)
			{
				sb.Append((char)strtab[off++]);
			}
			return sb.ToString();
		}

		public virtual SHeader sectionWithName(string name)
		{
			for (int i = 0;i < sheaders.Length;i++)
			{
				if (sheaders[i].name.Equals(name))
				{
					return sheaders[i];
				}
			}
			return null;
		}

		public class ELFException : IOException
		{
			private readonly ELF outerInstance;

			internal ELFException(ELF outerInstance, string s) : base(s)
			{
				this.outerInstance = outerInstance;
			}
		}

		private class SectionInputStream : InputStream
		{
			private readonly ELF outerInstance;

			internal int pos;
			internal int maxpos;
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: SectionInputStream(int start, int end) throws IOException
			internal SectionInputStream(ELF outerInstance, int start, int end)
			{
				this.outerInstance = outerInstance;
				if (outerInstance.sectionReaderActive)
				{
					throw new IOException("Section reader already active");
				}
				outerInstance.sectionReaderActive = true;
				pos = start;
				outerInstance.data.seek(pos);
				maxpos = end;
			}

			internal virtual int bytesLeft()
			{
				return maxpos - pos;
			}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int read() throws IOException
			public override int read()
			{
				sbyte[] buf = new sbyte[1];
				return read(buf,0,1) == -1 ? - 1 : (buf[0] & 0xff);
			}
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int read(byte[] b, int off, int len) throws IOException
			public override int read(sbyte[] b, int off, int len)
			{
				int n = outerInstance.data.read(b,off,Math.Min(len,bytesLeft()));
				if (n > 0)
				{
					pos += n;
				}
					return n;
			}
			public override void close()
			{
				outerInstance.sectionReaderActive = false;
			}
		}

		private SymbolTable _symtab;
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Symtab getSymtab() throws IOException
    public virtual SymbolTable Symtab
		{
			get
			{
				if (_symtab != null)
				{
					return _symtab;
				}
    
				if (sectionReaderActive)
				{
					throw new ELFException(this, "Can't read the symtab while a section reader is active");
				}
    
				SHeader sh = sectionWithName(".symtab");
				if (sh == null || sh.type != SHT_SYMTAB)
				{
					return null;
				}
    
				SHeader sth = sectionWithName(".strtab");
				if (sth == null || sth.type != SHT_STRTAB)
				{
					return null;
				}
    
				sbyte[] strtab = new sbyte[sth.size];
        InputStream dis = sth.InputStream;
				dis.tryReadFully(strtab,0,sth.size);
				dis.close();
    
				return _symtab = new SymbolTable(this, sh.offset, sh.size,strtab);
			}
		}

		public class SymbolTable
		{
			private readonly ELF outerInstance;

			public Symbol[] symbols;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Symtab(int off, int size, byte[] strtab) throws IOException
      internal SymbolTable(ELF outerInstance, int off, int size, sbyte[] strtab)
			{
				this.outerInstance = outerInstance;
				outerInstance.data.seek(off);
				int count = size / 16;
				symbols = new Symbol[count];
				for (int i = 0;i < count;i++)
				{
					symbols[i] = new Symbol(outerInstance, strtab);
				}
			}

			public virtual Symbol getSymbol(string name)
			{
				Symbol sym = null;
				for (int i = 0;i < symbols.Length;i++)
				{
					if (symbols[i].name.Equals(name))
					{
						if (sym == null)
						{
							sym = symbols[i];
						}
						else
						{
							Console.Error.WriteLine("WARNING: Multiple symbol matches for " + name);
						}
					}
				}
				return sym;
			}

			public virtual Symbol getGlobalSymbol(string name)
			{
				for (int i = 0;i < symbols.Length;i++)
				{
					if (symbols[i].binding == Symbol.STB_GLOBAL && symbols[i].name.Equals(name))
					{
						return symbols[i];
					}
				}
				return null;
			}
		}

		public class Symbol
		{
			private readonly ELF outerInstance;

			public string name;
			public int addr;
			public int size;
			public sbyte info;
			public sbyte type;
			public sbyte binding;
			public sbyte other;
			public short shndx;
			public SHeader sheader;

			public const int STT_FUNC = 2;
			public const int STB_GLOBAL = 1;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Symbol(byte[] strtab) throws IOException
			internal Symbol(ELF outerInstance, sbyte[] strtab)
			{
				this.outerInstance = outerInstance;
				name = outerInstance.getString(outerInstance.readInt(),strtab);
				addr = outerInstance.readInt();
				size = outerInstance.readInt();
				info = outerInstance.readByte();
				type = (sbyte)(info & 0xf);
				binding = (sbyte)(info >> 4);
				other = outerInstance.readByte();
				shndx = outerInstance.readShort();
			}
		}

		private static string toHex(int n)
		{
			return "0x" + Convert.ToString(n & 0xffffffffL, 16);
		}

		/*public static void main(String[] args) throws IOException {
		    ELF elf = new ELF(new Seekable.InputStream(new FileInputStream(args[0])));
		    System.out.println("Type: " + toHex(elf.header.type));
		    System.out.println("Machine: " + toHex(elf.header.machine));
		    System.out.println("Entry: " + toHex(elf.header.entry));
		    for(int i=0;i<elf.pheaders.length;i++) {
		        ELF.PHeader ph = elf.pheaders[i];
		        System.out.println("PHeader " + toHex(i));
		        System.out.println("\tOffset: " + ph.offset);
		        System.out.println("\tVaddr: " + toHex(ph.vaddr));
		        System.out.println("\tFile Size: " + ph.filesz);
		        System.out.println("\tMem Size: " + ph.memsz);
		    }
		    for(int i=0;i<elf.sheaders.length;i++) {
		        ELF.SHeader sh = elf.sheaders[i];
		        System.out.println("SHeader " + toHex(i));
		        System.out.println("\tName: " + sh.name);
		        System.out.println("\tOffset: " + sh.offset);
		        System.out.println("\tAddr: " + toHex(sh.addr));
		        System.out.println("\tSize: " + sh.size);
		        System.out.println("\tType: " + toHex(sh.type));
		    }
		    Symtab symtab = elf.getSymtab();
		    if(symtab != null) {
		        System.out.println("Symbol table:");
		        for(int i=0;i<symtab.symbols.length;i++)
		            System.out.println("\t" + symtab.symbols[i].name + " -> " + toHex(symtab.symbols[i].addr));
		    } else {
		        System.out.println("Symbol table: None");
		    }
		}*/
	}

}