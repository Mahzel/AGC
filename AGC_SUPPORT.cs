using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.QualityTools.UnitTestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace AGC_SUPPORT
{
	/// <summary>
	/// <para>Namespace <see cref="AGC_SUPPORT"/> contains classes to support AGC (Apollo Guidance Computer) emulation</para>
	///  <para><see cref="AGC_SUPPORT.sWord"/> manage 16b words used by the AGC</para>
	///  <para><see cref="AGC_SUPPORT.BANK"/> manage memory banks, ereasable or fixed, and I/O with the memory file</para>
	///  <para><see cref="AGC_SUPPORT.CLOCK"/> emulate a simple clock (that should tick every 12µs</para>
	///  <para><see cref="AGC_SUPPORT.AGC"/> emulate AGC functions : code processing, interrupt, task management</para>
	/// </summary>
	[System.Runtime.CompilerServices.CompilerGenerated]
	class NamespaceDoc
	{
	}

	/// <summary>
	/// <para>Memory library for the AGC/DSKY emulation</para>
	///  <para>sWord class : maintain a 16b word on both Hex(String) and Binary array from a short designed for the AGC usage</para>
	///  <para>Array Index :   15      14      13      12      11      10      9       8       7       6       5       4       3       2       1       0</para>
	///  <para>Bit position :  16      15      14      13      12      11      10      9       8       7       6       5       4       3       2       1</para>
	///  <para>Bit function :  P       S       D       D       D       D       D       D       D       D       D       D       D       D       D       D</para>
	///  <para>Fraction     :  -   -   2       4       8       16      32      64      128     256     512     1024    2048    4096    8192    16384   32768</para>
	///  <para>P : Parity Bit (if activated)</para>
	///  <para>S : Sign bit</para>
	///  <para>D : Data bit</para>
	///  <para> </para>
	///  <para>Data are treated as unsigned, but for AGC usage, bit 15-1 should be used as signed fixed point, with the point between 15-14.</para>
	///  <para>Displayed data should be corrected with the appropriate scaling</para>
	///  <para>0000 0000 0000 0000 : +0</para>
	///  <para>1111 1111 1111 1111 : -0</para>
	///  <para>1010 0000 0000 0000 : 1/2</para>
	///  <para>0110 0000 0000 0001 : -1/2</para>
	///  <para> </para>
	///  <para>ex : x.2^-6 00.00 000x 0000 0000</para>
	///  <para>     y.2^-4 00.00 0y00 0000 0000</para>
	///  <para>before addition or substract : shift one term to the same precision</para>
	///  <para>before multiplication or division, it's not needed. x*2^a * y*2^b = z*2^(a+b) // x*2^a / y*2^b = z*2^(a-b)</para>
	/// </summary>
	public class sWord
	{
		/// <summary>
		/// decimal value
		/// </summary>
		private short hex;
		/// <summary>
		/// binary (byte[16] array) value
		/// </summary>
		private byte[] word = new byte[16];
		/// <summary>
		/// parity bit option. TRUE = NO PARITY
		/// </summary>
		private String hexS;
		/// <summary>
		/// binary string "0b{array}"
		/// </summary>
		private String binS;
        private bool isNegative;

        //Constructors
		/// <summary>
		/// empty word 0x000, parity check
		/// </summary>
		public sWord ()
		{
			hex = 0x0000;
			word = hexToByte ();
			buildStr ();
		}
		/// <summary>
		/// Constructor for hex value only, parity is checked by default
		/// </summary>
		/// <param name="ihex">16b decimal word</param>
		public sWord (short ihex)
		{
			hex = ihex;
            if(hex<0)
            { isNegative = true; }
            word = hexToByte();
            calcParity();
		}
		/// <summary>
		/// Constructor for binary array, parity checked by default
		/// </summary>
		/// <param name="iWord">16b byte array</param>
		public sWord (byte[] iWord)
		{
			word = iWord;
            calcParity();
            negativeState();
			hex = byteToHex ();
		}

        //Conversion functions
		/// <summary>
		/// convert byte array to hex value (dec short)
		/// </summary>
		/// <returns>return the decimal (short) value of the byte array.</returns>
		public short byteToHex ()
		{
			hex = 0;
			for (int i = 0; i < 14; i++) {
				hex += (short)(word [i] * Math.Pow (2, i));           
			}
            if(isNegative)
            { hex = (short)(-hex); }
			return hex;
		}
		/// <summary>
		/// convert hex value to byte array
		/// </summary>
		/// <returns>return the byte array of the hex value.</returns>
		public byte[] hexToByte ()
		{
            byte[] tB = new byte[16];
            if(hex > 16383 | hex < -16382)
            {
                hex = Overflow(hex);
            }
			tB = toByteArray(hex);
            if (isNegative)
            { tB[14] = 1; }
			return tB;
		}

        public static byte[] toByteArray(short num)
        {
            int i = 0;
            byte[] tB = new byte[16];
            if (num < 0)
            { num = (short)(-num); }
            if (num != 0)
            {
                while (num != 0)
                {
                    if (num % 2 == 0)
                    {
                        tB[i] = 0;
                        num = (short)(num / 2);
                    }
                    else
                    {
                        tB[i] = 1;
                        num = (short)(num / 2);
                    }
                    i++;
                }
                for (int t = i; t <=15; t++)
                {
                    tB[t] = 0;
                }
            }
            else
            {
                for (i = 0; i <= 15; i++)
                {
                    tB[i] = 0;
                }
            }
            return tB;
        }

        public short Overflow(short hex)
        {
            while(hex>16383 | hex<-16383)
            {
                if(hex>0)
                { hex -= 16383; }
                else { hex += 16383; }
            }
            return (hex);
        }
        /// <summary>
        /// Generate the strings from hex value and binary array
        /// </summary>
        private void buildStr()
        {
            short tnum=0;
            binS = "";
            hexS = "";
            int[] tab = new int[15];
            for(int i = 0; i <= 14;i++ )
            {
                tnum += (short)(word[i] * Math.Pow(2, i));
                tab[i] = word[i];
            }
            hexS = String.Format("0x{0:X4}", tnum);
            Array.Reverse(tab);
            foreach(int x in tab)
            {
                binS += x;
            }
            binS = "0b" + binS;
        }
        private void negativeState()
        {
            if(word[14] == 1)
            {
                isNegative = true;
            }
            else
            {
                isNegative = false;
            }
        }
        private bool negativeState(byte [] bytes)
        {
            if (bytes[14] == 1)
            { return true; }
            else { return false; }
        }

        //Word operations
		/// <summary>
		/// Cycle Left the binary array (Shift 14-1 left, bit 15 -> 1)
		/// </summary>
		/// <returns>return cycled word, leave sWord intact</returns>
		public void CYL ()
		{
			byte[] tB = new byte[16];
			for (int i = 13; i >= 0; i--) {
				tB [i + 1] = word [i];
			}
			tB [0] = word [14];
            word = tB;
            negativeState();
            calcParity();
            byteToHex();
		}
		/// <summary>
		/// Shift right the binary array
		/// </summary>
		/// <returns>return shifted word, leave sWord intact</returns>
		public void SHR ()
		{
			byte[] tB = new byte[16];
			for (int i = 14; i >= 1; i--) {
				tB [i - 1] = word [i];
			}
			tB [14] = tB[13];
            word = tB;
            negativeState();
            calcParity();
            byteToHex();
		}
		/// <summary>
		/// Shift Left the binary array
		/// </summary>
		/// <returns>return shifted word, leave sWord intact</returns>
		public void SHL ()
		{
			byte[] tB = new byte[16];
			for (int i = 0; i < 14; i++) {
				tB [i + 1] = word [i];
			}
			tB [0] = 0;
            word = tB;
            negativeState();
            calcParity();
            byteToHex();
		}
		/// <summary>
		/// One's complement of the binary array
		/// </summary>
		/// <returns>return complemented word, leave sWord intact</returns>
		public void CPL ()
		{
			for (int i = 0; i < 15; i++) {
				if (word [i] == 0) {
					word [i] = 1;
				} else {
					word [i] = 0;
				}
			}
            negativeState();
            calcParity();
            byteToHex();

		}
		/// <summary>
		/// Cycle Right the binary array : Shift right 15-2, bit 1 -> 15
		/// </summary>
		/// <returns>return a cycled word, leave sWord intact</returns>
		public void CYR ()
		{
			byte[] tB = new byte[16];
			for (int i = 14; i >= 1; i--) {
				tB [i - 1] = word [i];
			}
			tB [14] = word [0];
            word = tB;
            negativeState();
			calcParity ();
            byteToHex();
		}
		/// <summary>
		/// calculate the parity bit, so the number of 1 is odd.
		/// </summary>
		/// <returns>byte array with the computed parity bit</returns>
		private byte[] calcParity ()
		{
			int oneB = 0;
			for (int i = 0; i < 15; i++) {
				if (word [i] == 1) {
					oneB++;
				}
			}
			if (oneB % 2 != 0) {
				word [15] = 1;
			} else {
				word [15] = 0;
			}
			return word;
		}
		/// <summary>
		/// check if the number of 1 is odd. If not, data may be corrupted
		/// </summary>
		/// <returns>true : data is complete</returns>
		public bool parity_check ()
		{
			int oneB = 0;
			for (int i = 0; i <= 15; i++) {
				if (word [i] == 1) {
					oneB++;
				}
			}
			if (oneB % 2 != 0) {
				return true;
			} else {
				return false;
			}
		}
        public int DABS()
        {
            int retVal = Math.Abs(getInt());
            if (retVal == 0)
            { return 0; }
            else
            { return retVal - 1; }
        }

        //geters
		/// <summary>
		/// get the Hex short value
		/// </summary>
		/// <returns>return (short)decimal value</returns>
		public short getHex ()
		{
			return hex;
		}
		/// <summary>
		/// get the binary array
		/// </summary>
		/// <returns>return a byte[16] containing the word bits</returns>
		public byte[] getWord ()
		{
			return word;
		}
        /// <summary>
        /// get the Hex String
        /// </summary>
        /// <returns>Hex String "0x{value}"</returns>
        public String getHexS()
        {
            buildStr();
            return hexS;
        }
        /// <summary>
        /// get the binary string
        /// </summary>
        /// <returns>Bin string "0b{value}"</returns>
        public String getBinS()
        {
            buildStr();
            return binS;
        }

        //special geters
        /// <summary>
        /// calculate the decimal value of the opcode and return it
        /// </summary>
        /// <returns>short opcode</returns>
        public short getOpCode()
        {
            return (short)(word[14] * 4 + word[13] * 2 + word[12]);
        }
        /// <summary>
        /// calculate the decimal value of the operand (adress) and return it
        /// </summary>
        /// <returns>short adress</returns>
        public short getOperand()
        {
            short retval = 0;
            for (int i = 11; i >= 0; i--)
            {
                retval += (short)(word[i] * Math.Pow(2, i));
            }
            return retval;
        }
        /// <summary>
        /// Get the decimal-equivalent of the specified binary range 0-15
        /// </summary>
        /// <param name="start">Lowest bit offset</param>
        /// <param name="end">Highest bit offset</param>
        /// <returns>The value of the binary range. Value is not shifted. Mean if you give : 10-15 it'll return as if it was a 0-5 binary word.</returns>
        public short getVal(int start, int end)
        {
            short ret = 0;
            for (int i = start; i <= end; i++)
            {
                ret += (short)(word[i] * Math.Pow(2, i - start));
            }
            return ret;
        }
        public int getInt()
        {
            return (int)hex;
        }

        //seters
		/// <summary>
		/// set Hex (short) value
		/// </summary>
		/// <param name="iHex">hex value to set (short)</param>
		public void setHex (short iHex)
		{
			hex = iHex;
			word = hexToByte ();
            negativeState();
		}
		/// <summary>
		/// set the binary array
		/// </summary>
		/// <param name="iw">the word to set (byte[16])</param>
		public void setWord (byte[] iw)
		{
			word = iw;
			hex = byteToHex ();
            negativeState();
		}

        public static byte[] orOps(byte[] t1, byte[] t2)
        {
            for(int i=0;i<=15;i++)
            {
                if(t2[i]==1)
                {
                    t1[i] = 1;
                }
            }
            return t1;
        }
	}

	/// <summary>
	/// <para>BANK class to load/write piece of memory</para>
	/// <para>BANK class, representing a Memory bank of words.</para>
	///<para>Erasable memory is 256 words bank</para>
	///<para>Fixed memory is 1024 words bank</para>
	/// </summary>
	public class BANK
	{
		/// <summary>
		/// If this flag is set, then all banks are writable. Use at your own risks.
		/// </summary>
		public Boolean compiling = false;
		/// <summary>
		/// size of the bank
		/// </summary>
		private short size;
		/// <summary>
		/// Is_Erasable? TRUE = E-MEM
		/// </summary>
		private bool is_ErType;
		/// <summary>
		/// Bank ID - E:0-8 / F:0-31
		/// </summary>
		private short bank_id;
		/// <summary>
		/// the MEM_ARRAY in decimal value
		/// </summary>
		private short[] MEM_ARRAY;
		/// <summary>
		/// Fixed Extention Bit. If 1, design Super Bank 32 to 35 (24+8 - 27+8)
		/// </summary>
		private int FEB;
		/// <summary>
		/// base adress in memory file of the bank
		/// </summary>
		private int b_adress;
		/// <summary>
		/// temp sWord for word manipulation
		/// </summary>
		private sWord temp;
		private String AGC_file;

		/// <summary>
		/// <para>Constructor for BANK.</para>
		/// <para>size : number of words in the bank.</para>
		/// <para>E-B is 256 - F-B is 1024</para>
		/// </summary>
		/// <param name="type">1-ERASABLE 0-FIXED</param>
		/// <param name="id">Register bank ID</param>
		/// <param name="SB">FEB value if applicable</param>
		/// <param name="file">The binary file to read/write.</param>
		public BANK (bool type, short id, int SB, String file)
		{
			is_ErType = type;
			AGC_file = file;
			FEB = SB;
			bank_id = id;
			byte[] bytestr = new byte[16];
			FileStream fs = File.Open (AGC_file, FileMode.OpenOrCreate);
			if (is_ErType) {
				size = 256;
			} else {
				size = 1024;
			}
			base_address ();
			MEM_ARRAY = new short[size];
			int j = 0;
			for (int i = 0; i < size; i++) {
				if (b_adress + j + 16 <= fs.Length) {
					fs.Seek (b_adress + j, SeekOrigin.Begin);
					fs.Read (bytestr, 0, 16);
					Array.Reverse (bytestr); //reversing the array so it's in good order
					temp = new sWord (bytestr);
					MEM_ARRAY [i] = temp.getHex ();
					j += 16;
				} else {   //if memory emplacement is empty in file (shouldn't), replace by zero.
					MEM_ARRAY [i] = 0x0;
				}
			}
			if (fs != null) {
				fs.Close ();
				fs.Dispose ();
			}            
		}

        //geters
		/// <summary>
		/// get the sWord at the given offset (in MEMORY ARRAY), building word from decimal value
		/// </summary>
		/// <param name="offset_index">offset index is the memory adress in the bank</param>
		/// <returns>the sWord at the given offset</returns>
		public sWord get_sword (short offset_index)
		{
			return new sWord (MEM_ARRAY [offset_index]);
		}
        public short get_word(short offset_index)
        {
            return MEM_ARRAY[offset_index];
        }
        /// <summary>
        /// return the base adress of the current bank
        /// </summary>
        /// <returns>The base adress in the file. To get the AGC adress, divide by 16.</returns>
        public int get_ba()
        {
            return b_adress;
        }
        /// <summary>
        /// return wether the bank is erasable
        /// </summary>
        /// <returns>TRUE : bank is Erasable</returns>
        public bool isErasable()
        {
            return is_ErType;
        }
        /// <summary>
        /// Return the bank id
        /// </summary>
        /// <returns>the bank Id</returns>
        public int getId()
        {
            return bank_id;
        }
        /// <summary>
        /// return the Fiexed Extention Bit value
        /// </summary>
        /// <returns>The FEB value is 0-1</returns>
        public int getFEB()
        {
            return FEB;
        }

        //seters
		/// <summary>
		/// Set the word at the given offset (in MEMORY ARRAY.), inserting the decimal value
		/// </summary>
		/// <param name="offset_index">offset is the memory adress in the bank</param>
		/// <param name="hex">the word value to write in the bank</param>
		/// <returns>0 : writed / 1 : bank is read only</returns>
		public int set_hex (short offset_index, short hex)
		{
			if (is_ErType | compiling) {
				MEM_ARRAY [offset_index] = hex;
				return 0;
			} else {
				return 1;
			}
		}

        public int set_sword(short offset_index, sWord word)
        {
            if (is_ErType | compiling)
            {
                MEM_ARRAY[offset_index] = word.getHex();
                return 0;
            }
            else
            {
                return 1;
            }
        }

        //file operation
		/// <summary>
		/// write the bank to the binary output file
		/// </summary>
		public void write_bank ()
		{
			if (is_ErType | compiling) { //only erasable bank is writable
				FileStream fs = File.OpenWrite (AGC_file);
				int j = 0;
				for (int i = 0; i < size; i++) {
					fs.Seek (b_adress + j, SeekOrigin.Begin);
					temp = new sWord (MEM_ARRAY [i]);
					byte[] tmp = temp.getWord ();
					Array.Reverse (tmp);
					fs.Write (tmp, 0, 16);
					j += 16;
				}
				fs.Close ();
				fs.Dispose ();
			}
		}
        public void write_word(int offset)
        {
            FileStream fs = File.OpenWrite(AGC_file);
            if (is_ErType)
            {
                fs.Seek(b_adress + offset, SeekOrigin.Begin);
                temp = new sWord(MEM_ARRAY[offset]);
                byte[] tmp = temp.getWord();
                Array.Reverse(tmp);
                fs.Write(tmp, 0, 16);
            }
            fs.Close();
            fs.Dispose();
        }

        //msciallaneous
		/// <summary>
		/// <para>compute the base adress of the bank</para>
		/// <para>base adress in bin file. AGC base adress is b_adress / 16;</para>
		/// </summary>
		private void base_address ()
		{
			if (!is_ErType) {
				if (FEB == 0) {
					switch (bank_id) {
					case 2:
						b_adress = ((bank_id * 1024) * 16);
						break;//(bank_id << 2) *100*16b
					case 3:
						b_adress = ((bank_id * 1024) * 16);
						break;//(bank_id << 2) *100*16b
					default:
						b_adress = (((bank_id * 1024) + 4096) * 16);
						break;//(bank_id << 2) * 100 + 0x1000 (0o10000) *16b
					}
				} else {
					b_adress = ((((bank_id + 8) * 1024) + 4096) * 16);//(((bank_id + 0x1000) << 2)*100+0x1000) * 16b
				}
			} else {
				b_adress = (bank_id * 256 * 16);//(id * e-bank size)*16b
			}
		}
	}

	/// <summary>
	/// <para>placeholder for a clock</para>    
	/// <para>clock : 97656ns / clock tick</para>
	/// </summary>
	public class CLOCK
	{
		/// <summary>
		/// Initial tick after a (re)start or if get_cycle returns >1
		/// </summary>
		long i_ticks;
		/// <summary>
		/// computed elapsed ticks
		/// </summary>
		long e_ticks;
		/// <summary>
		/// computed CPU cycles
		/// </summary>
		int cycle;

		/// <summary>
		/// clock placeholder
		/// </summary>
		public CLOCK ()
		{
			i_ticks = DateTime.Now.Ticks;
		}

		/// <summary>
		/// return the number of cycles since the last call
		/// </summary>
		/// <returns>returns the number of cycle since the last call</returns>
		public int get_cycle ()
		{
			e_ticks = DateTime.Now.Ticks - i_ticks;
			cycle = (int)(e_ticks / 120000);
			//Console.WriteLine("Cycle n° : {0} / Ticks : {1}", cycle, e_ticks);
			if (cycle >= 1) {
				i_ticks = DateTime.Now.Ticks;
				return cycle;
			} else {
				return 0;
			}
		}

		/// <summary>
		/// start/restart the clock
		/// </summary>
		public void c_start ()
		{
			i_ticks = DateTime.Now.Ticks;
		}
	}

	/// <summary>
	/// Fixed Values Dictionaries for the AGC
	/// </summary>
	public class fvDict
	{
		//Functions working with double point to word K instead of K+1 (as normally used in the AGC)
		/// <summary>
		/// contain the registers "text" value from an AGC Code
		/// </summary>
		public Dictionary<String, int> registers = new Dictionary<String, int> ();
		/// <summary>
		/// contains the standard opcodes and the corresponding decimal value.
		/// </summary>
		public Dictionary<String, short> opcode = new Dictionary<String, short> ();
		/// <summary>
		/// Contain standard quartercodes and the corresponding decimal value
		/// </summary>
		public Dictionary<String, short> quarter = new Dictionary<String, short> ();
		/// <summary>
		/// Contain extended opcodes and the corresponding decimal value
		/// </summary>
		public Dictionary<String, short> extrac = new Dictionary<String, short> ();
		/// <summary>
		/// Contain extended quartercodes and the corresponding decimal value
		/// </summary>
		public Dictionary<String, short> extraq = new Dictionary<String, short> ();
		/// <summary>
		/// Contain extended Implied Adress Codes and the corresponding decimal value
		/// </summary>
		public Dictionary<String, short> IACode = new Dictionary<String, short> ();
		/// <summary>
		/// Contain standard  I/O codes and the corresponding decimal value
		/// </summary>
		public Dictionary<String, short> IOCode = new Dictionary<String, short> ();

		/// <summary>
		/// Constructor for the Fixed Values Dictionary
		/// </summary>
		public fvDict ()
		{
			registers.Add ("rA", 0);
			registers.Add ("rZ", 5);
			registers.Add ("rL", 1);
			registers.Add ("rQ", 2);
			registers.Add ("rBB", 6);
			registers.Add ("rEB", 3);
			registers.Add ("rFB", 4);
			registers.Add ("rZERO", 7);
			opcode.Add ("TC", 0);
			opcode.Add ("CCS", 1);
			opcode.Add ("DAS", 2);
			opcode.Add ("CA", 3);
			opcode.Add ("CS", 4);
			opcode.Add ("INDEX", 5);
			opcode.Add ("AD", 6);
			opcode.Add ("MASK", 7);
			quarter.Add ("TCF", 4);
			quarter.Add ("LXCH", 9);
			quarter.Add ("INCR", 10);
			quarter.Add ("ADS", 11);
			quarter.Add ("DXCH", 21);
			quarter.Add ("TS", 22);
			quarter.Add ("XCH", 23);
			extrac.Add ("DV", 1);
			extrac.Add ("MSU", 2);
			extrac.Add ("DCA", 3);
			extrac.Add ("DCS", 4);
			extrac.Add ("INDEX", 5);
			extrac.Add ("SU", 6);
			extrac.Add ("MP", 7);
			extraq.Add ("BZF", 4);
			extraq.Add ("QXCH", 9);
			extraq.Add ("AUG", 10);
			extraq.Add ("DIM", 11);
			extraq.Add ("BZMF", 24);
			IACode.Add ("XXALQ", 0);
			IACode.Add ("XLQ", 1);
			IACode.Add ("RETURN", 2);
			IACode.Add ("RELINT", 3);
			IACode.Add ("INHINT", 4);
			IACode.Add ("EXTEND", 6);
			IACode.Add ("DDOUBL", 8192);
			IACode.Add ("ZL", 9223);
			IACode.Add ("COM", 16384);
			IACode.Add ("DTCF", 21508);
			IACode.Add ("DTCB", 21509);
			IACode.Add ("OVSK", 22528);
			IACode.Add ("TCAA", 22533);
			IACode.Add ("DOUBLE", 24576);
			IACode.Add ("ZQ", 9223);
			IACode.Add ("DCOM", 16385);
			IACode.Add ("SQUARE", 28672);
			IACode.Add ("RESUME", 20495);
			IOCode.Add ("READ", 0);
			IOCode.Add ("WRITE", 1);
			IOCode.Add ("RAND", 2);
			IOCode.Add ("WAND", 3);
			IOCode.Add ("ROR", 4);
			IOCode.Add ("WOR", 5);
			IOCode.Add ("RXOR", 6);
			IOCode.Add ("WXOR", 7);
		}
	}

    public class Channels
    {
        private short[] AChannels;
        public delegate void dChannel(int channel);
        public dChannel AGCList;
        public dChannel DSKYList;
        public dChannel other;

        public Channels()
        {
            AChannels = new short[10];
            for (int i = 0; i < 10; i++)
            { AChannels[i] = 0; }
        }

        public void notify(object sender, int chan)
        {

            if (sender.ToString() == "nAGC.AGC")
            {
                if (DSKYList != null)
                {
                    DSKYList(chan);
                }
            }
            else if (sender.ToString() == "nDSKY.tDSKY")
            {
                if (AGCList != null)
                {
                    AGCList(chan);
                }
            }
            if (other != null)
            {
                other(chan);
            }

        }

        public void regDelegate(object sender, dChannel obs)
        {
            if (sender.ToString() == "nAGC.AGC")
            {
                if (AGCList == null)
                { AGCList = new dChannel(obs); }
                else
                {
                    AGCList += obs;
                }
            }
            else if(sender.ToString()=="nDSKY.tDSKY")
            {
                if (DSKYList == null)
                { DSKYList = new dChannel(obs); }
                else
                {
                    DSKYList += obs;
                }
            }
            else
            {
                if (other == null)
                { other = new dChannel(obs); }
                else
                {
                    other += obs;
                }
            }
        }

        public void set_chan(object sender, int index, short value)
        {
            if ((index < 10) && (index >= 0))
            { AChannels[index] = value;
              notify(sender, index);
            }
            else
            { Console.WriteLine("index {0} out of channel range", index); }
        }

        public short get_chan(int index)
        {
            if ((index < 10) && (index >= 0))
            { return AChannels[index]; }
            else
            {
                Console.WriteLine("index {0} out of channel range", index);
                return 0;
            }
        }
    }

    [TestClass]
    public class SUPPORT_TEST
    {
        sWord Test = new sWord();

        [TestMethod]
        public void TestNegative()
        {
            Test.setHex(0x4000);
            int expected = -16383;

            int result = Test.getInt();

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void DABS()
        {
            Test.setHex(0x4000);
            int expected = 16382;

            int result = Test.DABS();

            Assert.AreEqual(expected, result);
        }
    }

}
