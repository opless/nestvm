using System;

// Copyright 2000-2005 the Contributors, as shown in the revision logs.
// Licensed under the Apache License 2.0 ("the License").
// You may not use this file except in compliance with the License.

namespace org.ibex.nestedvm.util
{

	// Based on the various org.xwt.util.* classes by Adam Megacz

	public class InodeCache
	{
		private static readonly object PLACEHOLDER = new object();
		private const short SHORT_PLACEHOLDER = -2;
		private const short SHORT_NULL = -1;
		private const int LOAD_FACTOR = 2;

		private readonly int maxSize;
		private readonly int totalSlots;
		private readonly int maxUsedSlots;

		private readonly object[] keys;
		private readonly short[] next;
		private readonly short[] prev;
		private readonly short[] inodes;
		private readonly short[] reverse_Renamed;

		private int size, usedSlots;
		private short mru, lru;

		public InodeCache() : this(1024)
		{
		}
		public InodeCache(int maxSize)
		{
			this.maxSize = maxSize;
			totalSlots = maxSize * LOAD_FACTOR * 2 + 3;
			maxUsedSlots = totalSlots / LOAD_FACTOR;
			if (totalSlots > short.MaxValue)
			{
				throw new System.ArgumentException("cache size too large");
			}
			keys = new object[totalSlots];
			next = new short[totalSlots];
			prev = new short[totalSlots];
			inodes = new short[totalSlots];
			reverse_Renamed = new short[totalSlots];
			clear();
		}

		private static void fill(object[] a, object o)
		{
			for (int i = 0;i < a.Length;i++)
			{
				a[i] = o;
			}
		}
		private static void fill(short[] a, short s)
		{
			for (int i = 0;i < a.Length;i++)
			{
				a[i] = s;
			}
		}
		public void clear()
		{
			size = usedSlots = 0;
			mru = lru = -1;
			fill(keys,null);
			fill(inodes,SHORT_NULL);
			fill(reverse_Renamed,SHORT_NULL);
		}

		public short get(object key)
		{
			int hc = key.GetHashCode() & 0x7fffffff;
			int dest = hc % totalSlots;
			int odest = dest;
			int tries = 1;
			bool plus = true;
			object k;
			int placeholder = -1;

			while ((k = keys[dest]) != null)
			{
				if (k == PLACEHOLDER)
				{
					if (placeholder == -1)
					{
						placeholder = dest;
					}
				}
				else if (k.Equals(key))
				{
					short inode = inodes[dest];
					if (dest == mru)
					{
						return inode;
					}
					if (lru == dest)
					{
						lru = next[lru];
					}
					else
					{
						short p = prev[dest];
						short n = next[dest];
						next[p] = n;
						prev[n] = p;
					}
					prev[dest] = mru;
					next[mru] = (short) dest;
					mru = (short) dest;
					return inode;
				}
				dest = Math.Abs((odest + (plus ? 1 : -1) * tries * tries) % totalSlots);
				if (!plus)
				{
					tries++;
				}
				plus = !plus;
			}

			// not found
			int slot;
			if (placeholder == -1)
			{
				// new slot
				slot = dest;
				if (usedSlots == maxUsedSlots)
				{
					clear();
					return get(key);
				}
				usedSlots++;
			}
			else
			{
				// reuse a placeholder
				slot = placeholder;
			}

			if (size == maxSize)
			{
				// cache is full
				keys[lru] = PLACEHOLDER;
				inodes[lru] = SHORT_PLACEHOLDER;
				lru = next[lru];
			}
			else
			{
				if (size == 0)
				{
					lru = (short) slot;
				}
				size++;
			}

			int inode2;
			for (inode2 = hc & 0x7fff;;inode2++)
			{
				dest = inode2 % totalSlots;
				odest = dest;
				tries = 1;
				plus = true;
				placeholder = -1;
				int r;
				while ((r = reverse_Renamed[dest]) != SHORT_NULL)
				{
					int i = inodes[r];
					if (i == SHORT_PLACEHOLDER)
					{
						if (placeholder == -1)
						{
							placeholder = dest;
						}
					}
					else if (i == inode2)
					{
						goto OUTERContinue;
					}
					dest = Math.Abs((odest + (plus ? 1 : -1) * tries * tries) % totalSlots);
					if (!plus)
					{
						tries++;
					}
					plus = !plus;
				}
				// found a free inode
				if (placeholder != -1)
				{
					dest = placeholder;
				}
				goto OUTERBreak;
				OUTERContinue:;
			}
			OUTERBreak:
			keys[slot] = key;
			reverse_Renamed[dest] = (short) slot;
			inodes[slot] = (short) inode2;
			if (mru != -1)
			{
				prev[slot] = mru;
				next[mru] = (short) slot;
			}
			mru = (short) slot;
			return (short) inode2;
		}

		public virtual object reverse(short inode)
		{
			int dest = inode % totalSlots;
			int odest = dest;
			int tries = 1;
			bool plus = true;
			int r;
			while ((r = reverse_Renamed[dest]) != SHORT_NULL)
			{
				if (inodes[r] == inode)
				{
					return keys[r];
				}
				dest = Math.Abs((odest + (plus ? 1 : -1) * tries * tries) % totalSlots);
				if (!plus)
				{
					tries++;
				}
				plus = !plus;
			}
			return null;
		}

		/*private void dump() {
		    System.err.println("Size " + size);
		    System.err.println("UsedSlots " + usedSlots);
		    System.err.println("MRU " + mru);
		    System.err.println("LRU " + lru);
		    if(size == 0) return;
		    for(int i=mru;;i=prev[i]) {
		        System.err.println("" + i + ": " + keys[i] + " -> " + inodes[i] + "(prev: " + prev[i] + " next: " + next[i] + ")");
		        if(i == lru) break;
		    }
		}
		
		private void stats() {
		    int freeKeys = 0;
		    int freeReverse = 0;
		    int placeholderKeys = 0;
		    int placeholderReverse = 0;
		    for(int i=0;i<totalSlots;i++) {
		        if(keys[i] == null) freeKeys++;
		        if(keys[i] == PLACEHOLDER) placeholderKeys++;
		        if(reverse[i] == SHORT_NULL) freeReverse++;
		    }
		    System.err.println("Keys: " + freeKeys + "/" + placeholderKeys);
		    System.err.println("Reverse: " + freeReverse);
		}
		
		public static void main(String[] args) throws Exception {
		    InodeCache c = new InodeCache();
		    java.io.BufferedReader br = new java.io.BufferedReader(new java.io.InputStreamReader(System.in)); 
		    String s;
		    boolean good = false;
		    try {
		        while((s = br.readLine()) != null) {
		            if(s.charAt(0) == '#') {
		                short n = Short.parseShort(s.substring(1));
		                    System.err.println("" + n + " -> " + c.reverse(n));
		            } else {
		                //System.err.println("Adding " + s);
		                short n = c.get(s);
		                System.err.println("Added " + s + " -> " + n);
		                //c.dump();
		            }
		        }
		        good = true;
		    } finally {
		        if(!good) c.stats();
		    }
		}*/
	}

}