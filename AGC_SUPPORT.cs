using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
    ///  <para>sWord class : maintain a 16b word on both Hex(String) and Binary array from a ushort designed for the AGC usage</para>
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
        private ushort hex;
        /// <summary>
        /// binary (byte[16] array) value
        /// </summary>
        private byte[] word = new byte[16];
        /// <summary>
        /// parity bit option. TRUE = NO PARITY
        /// </summary>
        bool no_parity;
        /// <summary>
        /// hex string "0x{value}"
        /// </summary>
        private String hexS;
        /// <summary>
        /// binary string "0b{array}"
        /// </summary>
        private String binS;
        /// <summary>
        /// empty word 0x000, parity check
        /// </summary>
        public sWord()
        {hex = 0x0000;
        no_parity = false;
        word = hexToByte();
        buildStr();
        }
/// <summary>
/// Constructor for hex value only, parity is checked by default
/// </summary>
/// <param name="ihex">16b decimal word</param>
        public sWord(ushort ihex)
        {hex = ihex;
        no_parity = false;
        word = hexToByte();
        buildStr();
        }
/// <summary>
/// Constructor for binary array, parity checked by default
/// </summary>
/// <param name="iWord">16b byte array</param>
        public sWord(byte[] iWord)
        { word = iWord;
        no_parity = false;
        hex = byteToHex();
        parity();
        buildStr();
        }
/// <summary>
/// Constructor for hex word, with parity bit calculation option
/// </summary>
/// <param name="ihex">hex word</param>
/// <param name="no_par">FALSE = parity bit enabled</param>
        public sWord(ushort ihex, bool no_par)
        {hex = ihex;
        no_parity = no_par;
        word = hexToByte();
        buildStr();
        }
/// <summary>
/// Constructor for binary array, with parity bit calculation option
/// </summary>
/// <param name="iWord">16b word array</param>
/// <param name="no_par">FALSE = parity bit enabled</param>
        public sWord(byte[] iWord, bool no_par)
        {
            word = iWord;
            no_parity = no_par;
            hex = byteToHex();
            if (!no_par)
            { word = parity(); }
            buildStr();
        }
/// <summary>
/// Generate the strings from hex value and binary array
/// </summary>
        private void buildStr()
        { var builder = new StringBuilder();
        byte[] tb = new byte[16];
        tb = word;
        Array.ForEach(tb, x => builder.Append(x));
        char[] ta = builder.ToString().ToCharArray();
        Array.Reverse(ta);
        binS = new String(ta);
        binS = String.Format("0b{0}", binS);
        hexS = String.Format("0x{0:X4}", hex);
        }
/// <summary>
/// convert byte array to hex value (dec ushort)
/// </summary>
/// <returns>return the decimal (ushort) value of the byte array.</returns>
        public ushort byteToHex()
        {
            hex = 0;
            for (int i = 0; i < 16; i++ )
            { hex += (ushort)(word[i]*Math.Pow(2, i));           
            }
            return hex;
        }
/// <summary>
/// convert hex value to byte array
/// </summary>
/// <returns>return the byte array of the hex value.</returns>
        public byte[] hexToByte()
        {   int i = 0;
            int tHex = hex;
            byte[] tB = new byte[16];
            if(tHex !=0){
                while(tHex != 0){         
                    if (tHex % 2 == 0){
                        tB[i] = 0;
                        tHex = (ushort)(tHex / 2);
                     }
                    else {
                        tB[i] = 1;
                        tHex = (ushort)(tHex / 2);
                    }
                    i++;
                }
                for(int t = i; t<16; t++){
                    tB[t]=0;
                }
            }
            else{
                for(i = 0; i<=15; i++){
                    tB[i] = 0;
                }
            }
            if(!no_parity)
            { tB = parity(); }
            return tB;
        }
/// <summary>
/// Cycle Left the binary array (Shift 14-1 left, bit 15 -> 1)
/// </summary>
/// <returns>return cycled word, leave sWord intact</returns>
        public byte[] CYL()
        {
            byte[] tB = new byte[16];
            for (int i = 13; i >= 0; i--)
            {
                tB[i + 1] = word[i];
            }
            tB[0] = word[13];
            tB = parity();
            return tB;
        }
/// <summary>
/// Shift right the binary array
/// </summary>
/// <returns>return shifted word, leave sWord intact</returns>
        public byte[] SHR()
        {
            byte[] tB = new byte[16];
            for (int i = 14; i >= 1; i--)
            {
                tB[i - 1] = word[i];
            }
            tB[14] = word[14];
            tB = parity();
            return tB;
        }
/// <summary>
/// Shift Left the binary array
/// </summary>
/// <returns>return shifted word, leave sWord itnact</returns>
        public byte[] SHL()
        {
            byte[] tB = new byte[16];
            for (int i = 0; i < 14; i++)
            {
                tB[i+1] = word[i];
            }
            tB[0] = 0;
            tB = parity();
            return tB;
        }
/// <summary>
/// One's complement of the binary array
/// </summary>
/// <returns>return complemented word, leave sWord intact</returns>
        public byte[] CPL()
        {
            byte[] tB = new byte[16];
            for (int i = 0; i < 15; i++)
            {
                if (word[i] == 0)
                { tB[i] = 1; }
                else
                { tB[i] = 0; }
            }
            tB = parity();
            return tB;
        }
/// <summary>
/// Cycle Right the binary array : Shift right 15-2, bit 1 -> 15
/// </summary>
/// <returns>return a cycled word, leave sWord intact</returns>
        public byte[] CYR()
        {
            byte[] tB = new byte[16];
            for (int i = 13; i >= 1; i--)
            {
                tB[i - 1] = word[i];
            }
            tB[14] = word[0];
            tB = parity();
            return tB;
        }
/// <summary>
/// calculate the parity bit, so the number of 1 is odd.
/// </summary>
/// <returns>byte array with the computed parity bit</returns>
        private byte[] parity()
        {
            int oneB = 0;
            for (int i = 0; i < 15; i++ )
            {
                if(word[i] == 1)
                {oneB++;}
            }
            if(oneB%2 != 0)
            {word[15] = 1;}
            else { word[15] = 0; }
            return word;
        }
/// <summary>
/// check if the number of 1 is odd. If not, data may be corrupted
/// </summary>
/// <returns>true : data is complete</returns>
        public bool parity_check()
        {
            int oneB = 0;
            for (int i = 0; i <= 15; i++)
            {
                if (word[i] == 1)
                { oneB++; }
            }
            if (oneB % 2 != 0)
            { return true; }
            else { return false; }
        }
/// <summary>
/// get the Hex ushort value
/// </summary>
/// <returns>return (ushort)decimal value</returns>
        public ushort getHex()
        { return hex; }
/// <summary>
/// get wether the parity bit should be set or not
/// </summary>
/// <returns>true : don't check the parity</returns>
        public bool getPar()
        { return no_parity; }
/// <summary>
/// get the binary array
/// </summary>
/// <returns>return a byte[16] containing the word bits</returns>
        public byte[] getWord()
        { return word; }
/// <summary>
/// set Hex (ushort) value
/// </summary>
/// <param name="iHex">hex value to set (ushort)</param>
        public void setHex(ushort iHex)
        { hex = iHex;
        hexToByte();
        }
/// <summary>
/// set the binary array
/// </summary>
/// <param name="iw">the word to set (byte[16])</param>
        public void setWord(byte[] iw)
        { word = iw;
        byteToHex();
        }
/// <summary>
/// set wether the parity bit should be set or not
/// if set, perform the parity check.
/// if NOT, parity bit is set to 0.
/// </summary>
/// <param name="np">true : don't check parity</param>
        public void setPar(bool np)
        { no_parity = np;
        if (!np) { parity(); }
        else { word[15] = 0; }
        }
/// <summary>
/// calculate the decimal value of the opcode and return it
/// </summary>
/// <returns>ushort opcode</returns>
        public ushort getOpCode()
        {
            return (ushort)(word[14] * 4 + word[13] * 2 + word[12]);
        }
/// <summary>
/// calculate the decimal value of the operand (adress) and return it
/// </summary>
/// <returns>ushort adress</returns>
        public ushort getOperand()
        {
            ushort retval = 0;
            for (int i = 11; i >= 0;i-- )
            { retval += (ushort)(word[i] * Math.Pow(2, i)); }
                return retval;
        }
        public ushort getVal(int start, int end)
        {
            ushort ret = 0;
            for (int i = start; i <= end; i++)
            {
                ret += (ushort)(word[i] * Math.Pow(2, i-start));
            }
            return ret;
        }
        /// <summary>
        /// get the Hex String
        /// </summary>
        /// <returns>Hex String "0x{value}"</returns>
        public String getHexS()
        { return hexS; }
/// <summary>
/// get the binary string
/// </summary>
/// <returns>Bin string "0b{value}"</returns>
        public String getBinS()
        { return binS; }

    }
    /// <summary>
    /// <para>BANK class to load/write piece of memory</para>
    /// <para>BANK class, representing a Memory bank of words.</para>
    ///<para>Erasable memory is 256 words bank</para>
    ///<para>Fixed memory is 1024 words bank</para>
/// </summary>
    public class BANK
    {
        public Boolean compiling = false;
        /// <summary>
        /// size of the bank
        /// </summary>
        private ushort size;
        /// <summary>
        /// Is_Erasable? TRUE = E-MEM
        /// </summary>
        private bool is_ErType;
        /// <summary>
        /// Bank ID - E:0-8 / F:0-31
        /// </summary>
        private ushort bank_id;
        /// <summary>
        /// the MEM_ARRAY in decimal value
        /// </summary>
        private ushort[] MEM_ARRAY;
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
        public BANK(bool type, ushort id, int SB, String file)
        {
            is_ErType = type;
            AGC_file = file;
            FEB = SB;
            bank_id = id;
            byte[] bytestr = new byte[16];
            FileStream fs = File.Open(AGC_file, FileMode.OpenOrCreate);
            if(is_ErType)
            { size = 256;
            }
            else
            { size = 1024;}
            base_address();
            MEM_ARRAY = new ushort[size];
            int j = 0;
            for (int i = 0; i < size; i++)
            {
                if (b_adress + j + 16 <= fs.Length)
                {
                    fs.Seek(b_adress + j, SeekOrigin.Begin);
                    fs.Read(bytestr, 0, 16);
                    Array.Reverse(bytestr); //reversing the array so it's in good order
                    temp = new sWord(bytestr);
                    MEM_ARRAY[i] = temp.getHex();
                    j += 16;
                }
                else
                {   //if memory emplacement is empty in file (shouldn't), replace by zero.
                    MEM_ARRAY[i] = 0x0;
                }
            }
            if(fs != null)
            { fs.Close();
            fs.Dispose();
            }            
        }
        /// <summary>
        /// get the sWord at the given offset (in MEMORY ARRAY), building word from decimal value
        /// </summary>
        /// <param name="offset_index">offset index is the memory adress in the bank</param>
        /// <returns>the sWord at the given offset</returns>
        public sWord get_word(ushort offset_index)
        { return new sWord(MEM_ARRAY[offset_index], true); }
        /// <summary>
        /// Set the word at the given offset (in MEMORY ARRAY.), inserting the decimal value
        /// </summary>
        /// <param name="offset_index">offset is the memory adress in the bank</param>
        /// <param name="w">the sWord to write in the bank</param>
        /// <returns>0 : writed / 1 : bank is read only</returns>
        public int set_sword(ushort offset_index, sWord w)
        {
            if (is_ErType | compiling)
            {
            MEM_ARRAY[offset_index] = w.getHex();
            return 0;
            }
            else { return 1; }
        }
        /// <summary>
        /// write the bank to the binary output file
        /// </summary>
        public void write_bank()
        {
            if (is_ErType | compiling) //only erasable bank is writable
            {
                FileStream fs = File.OpenWrite(AGC_file);
                int j = 0;
                for (int i = 0; i < size; i++)
                {
                    fs.Seek(b_adress + j, SeekOrigin.Begin);
                    temp = new sWord(MEM_ARRAY[i], true);
                    byte[] tmp = temp.getWord();
                    Array.Reverse(tmp);
                    fs.Write(tmp, 0, 16);
                    j += 16;
                }
                fs.Close();
                fs.Dispose();
            }
        }
        /// <summary>
        /// <para>compute the base adress of the bank</para>
        /// <para>base adress in bin file. AGC base adress is b_adress / 16;</para>
        /// </summary>
        private void base_address()
        {
            if (!is_ErType)
            {
                if (FEB == 0)
                {
                    switch (bank_id)
                    {
                        case 2: b_adress = ((bank_id * 1024) * 16); break;//(bank_id << 2) *100*16b
                        case 3: b_adress = ((bank_id * 1024) * 16); break;//(bank_id << 2) *100*16b
                        default: b_adress = (((bank_id * 1024)+4096)*16);break;//(bank_id << 2) * 100 + 0x1000 (0o10000) *16b
                    }
                }
                else{
                    b_adress = ((((bank_id + 8) * 1024) + 4096) * 16);//(((bank_id + 0x1000) << 2)*100+0x1000) * 16b
                }
            }
            else
            {
                b_adress = (bank_id * 256*16);//(id * e-bank size)*16b
            }
        }

        public int get_ba()
        {
            return b_adress;
        }

        public bool isErasable()
        { return is_ErType; }

        public int getId()
        { return bank_id; }

        public int getFEB()
        { return FEB; }
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
        public CLOCK()
        {
            i_ticks = DateTime.Now.Ticks;
        }
        /// <summary>
        /// return the number of cycles since the last call
        /// </summary>
        /// <returns>returns the number of cycle since the last call</returns>
        public int get_cycle()
        {
            e_ticks = DateTime.Now.Ticks - i_ticks;
            cycle = (int)(e_ticks/120000);
            //Console.WriteLine("Cycle n° : {0} / Ticks : {1}", cycle, e_ticks);
            if(cycle >=1)
            {i_ticks = DateTime.Now.Ticks;
             return cycle;}
            else{return 0;}
        }
        /// <summary>
        /// start/restart the clock
        /// </summary>
        public void c_start()
        {
            i_ticks = DateTime.Now.Ticks;
        }
    }
    /// <summary>
/// placeholder for an AGC with basic registries
/// </summary>
    public class AGC
    {
        /// <summary>
        /// number of CPU cycle to manage MCT
        /// </summary>
        int cycle_count;
        /// <summary>
        /// Fixed Bank 5b register
        /// </summary>
        ushort FB;
        /// <summary>
        /// Erasable Bank 3 bit register
        /// </summary>
        ushort EB;
        /// <summary>
        /// Fixed Extention Bit Signal
        /// </summary>
        sWord FEB; //channel 07
        /// <summary>
        /// Target adress register
        /// </summary>
        ushort S;
        /// <summary>
        /// Accumulator register
        /// </summary>
        sWord A;
        /// <summary>
        /// current instruction register
        /// </summary>
        sWord Z;
        /// <summary>
        /// TC return register
        /// </summary>
        sWord Q;
        /// <summary>
        /// Lower adress regiser
        /// </summary>
        sWord L;
        /// <summary>
        /// 4b sequence reg - current instruction
        /// </summary>
        ushort SQ;
        /// <summary>
        /// 16b memory buffer to hold data
        /// </summary>
        sWord G;
        /// <summary>
        /// Input to CPL / adder first input
        /// </summary>
        sWord X;
        /// <summary>
        /// second adder input
        /// </summary>
        sWord Y;
        /// <summary>
        /// CPL of X.Y
        /// </summary>
        sWord U;
        /// <summary>
        /// pre-fetch next instruction - upper bit go to SQ, lower to S
        /// </summary>
        sWord B;
        /// <summary>
        /// 4 word input register
        /// </summary>
        sWord[] IN = new sWord[4];
        /// <summary>
        /// 4word output register
        /// </summary>
        sWord[] OUT = new sWord[4];
        /// <summary>
        /// running state
        /// </summary>
        bool running;
        /// <summary>
        /// AGC clock
        /// </summary>
        CLOCK clock;
        /// <summary>
        /// BANK in use
        /// </summary>
        BANK b;
        String AGC_File;
        /// <summary>
        /// Quarter code holding variable
        /// </summary>
        ushort QC;
        /// <summary>
        /// AGC builder placeholder
        /// </summary>
        public AGC(String file)
        {
            A = new sWord(0x0, false);
            Z = new sWord(0x0, false);
            Q = new sWord(0x0, false);
            L = new sWord(0x0, false);
            G = new sWord(0x0, false);
            X = new sWord(0x0, false);
            Y = new sWord(0x0, false);
            U = new sWord(0x0, false);
            B = new sWord(0x0, false);
            FEB = new sWord(0x0, false);
            FB = 0; EB = 0; S = 0; SQ = 0;
            running = false;
            clock = new CLOCK();
            b = null;
            cycle_count = 0;
            AGC_File = file;
            
        }
        /// <summary>
        /// start (run) a previous AGC intialized (powered up)
        /// </summary>
        public void start()
        {
            if(!running)
            {
                clock.c_start();
                cycle_count = 0;
                running = true;
                b = new BANK(false, FB, FEB.getHex(), AGC_File);
                Console.WriteLine("AGC Started");
                B = b.get_word(Z.getHex());
                B.setWord(B.hexToByte());
                Z.setHex((ushort)(Z.getHex() + 1));
                MCT();
            }
            else
            {
                Console.WriteLine("AGC Halted.");
            }
        }

        private void MCT()
        {
            while (running)
            {
                clock.c_start();
                cycle_count = 0;
                //int prev_cycle = 0;
                exec_opc(false);
                B = b.get_word(Z.getHex());
                B.setWord(B.hexToByte());
                Z.setHex((ushort)(Z.getHex() + 1));
               /* while (cycle_count < 13)
                {
                    cycle_count += clock.get_cycle();
                    if (cycle_count != prev_cycle)
                    { Console.WriteLine("Cycle n° : {0}", cycle_count); }
                    prev_cycle = cycle_count;
                }*/
            }
            Console.WriteLine("AGC halted");
        }

        /// <summary>
        /// <para>edit adress registry (FB/EB/FEB/S) based on the bits 12-1 of the current instruction</para>
        /// <para>detect usage of FB/EB register</para>
        /// <para>if not needed, will set them accordingly to the bank to load</para>
        /// </summary>
        /// <param name="adress">byte[] : 16b adress word</param>
        /// <returns>bool : type (true : erasable, false : fixed)</returns>
        public bool build_adress_reg(byte[] adress)
        {
            S = 0;
            if (adress[11] == 0 && adress[10] == 0)
            {   if(adress[9]!=1 && adress[8]!=1) // not to be normally used
                {EB = (ushort)(adress[9] * 2 + adress[8]);}

                for (int i = 0; i <= 7; i++)
                { S += (ushort)(adress[i] * Math.Pow(2, i)); }
                return true;
            }
            else if (adress[11] == 1)
            {
                FB = (ushort)(adress[11] * 2 + adress[10]);
                for (int i = 0; i <= 9; i++)
                { S += (ushort)(adress[i] * Math.Pow(2, i)); }
                return false;
            }
            else
            {
                for (int i = 0; i <= 9; i++)
                { S += (ushort)(adress[i] * Math.Pow(2, i));
                S -= 0x800;}//building offset -0x800 because bit 1 is nescessary in bit 12 in order for AGC to recognize a F-Switchable state
                return false;
            }
        }
        /// <summary>
        /// exec opcode routine
        /// disassamble word, find opcode then call function
        /// </summary>
        /// <param name="extra">Extracode instruction</param>
        public void exec_opc(Boolean extra)
        {
            SQ = B.getOpCode();
            S = B.getOperand();
            QC =(ushort)( B.getVal(10,11));
            if (!extra)
            {
                switch (SQ)
                {
                    case 0: Console.WriteLine("TC {0:X4}", S); break;
                    case 1: if (QC == 0)
                        {
                            Console.WriteLine("CCS {0:X4}", S); break;
                        }
                        else
                        {
                            S = B.getVal(0, 9); Console.WriteLine("TCF {0:X4}", S); break;
                        }
                    case 2: switch (QC)
                        {
                            case 0: Console.WriteLine("DAS {0:X4}", S); break;
                            case 1: S = B.getVal(0, 9); Console.WriteLine("LXCH {0:X4}", S); break;
                            case 2: S = B.getVal(0, 9); Console.WriteLine("INCR {0:X4}", S); break;
                            case 3: S = B.getVal(0, 9); Console.WriteLine("ADS {0:X4}", S); break;
                        }break;
                    case 3: Console.WriteLine("CA {0:X4}", S); break;
                    case 4: Console.WriteLine("CS {0:X4}", S); break;
                    case 5: switch (QC)
                        {
                            case 0: Console.WriteLine("INDEX {0:X4}", S); break;
                            case 1: S = B.getVal(0, 9); Console.WriteLine("DXCH {0:X4}", S); break;
                            case 2: S = B.getVal(0, 9); Console.WriteLine("TS {0:X4}", S); break;
                            case 3: S = B.getVal(0, 9); Console.WriteLine("XCH {0:X4}", S); break;
                        } break;
                    case 6: Console.WriteLine("AD {0:X4}", S); break;
                    case 7: Console.WriteLine("MASK {0:X4}", S); running = false; break;
                }
            }
        }
    }

    public class fvDict
    {
        public Dictionary<String, int> registers = new Dictionary<String, int>();
        public Dictionary<String, ushort> opcode = new Dictionary<String, ushort>();
        public Dictionary<String, ushort> quarter = new Dictionary<String, ushort>();
        public Dictionary<String, ushort> extrac = new Dictionary<String, ushort>();
        public Dictionary<String, ushort> extraq = new Dictionary<String, ushort>();
        public Dictionary<String, ushort> IACode = new Dictionary<String, ushort>();
        public Dictionary<String, ushort> IOCode = new Dictionary<String, ushort>();

        public fvDict(){
            registers.Add("rA", 0);
            registers.Add("rZ", 5);
            registers.Add("rL", 1);
            registers.Add("rQ", 2);
            registers.Add("rBB", 6);
            registers.Add("rEB", 3);
            registers.Add("rFB", 4);
            registers.Add("rZERO", 7);
            opcode.Add("TC", 0);
            opcode.Add("CCS", 1);
            opcode.Add("DAS", 2);
            opcode.Add("CA", 3);
            opcode.Add("CS", 4);
            opcode.Add("INDEX", 5);
            opcode.Add("AD", 6);
            opcode.Add("MASK", 7);
            //quarter.Add("TCF", 5); //TODO : special case TCF jump to 12b adress => QC depend of adress, not fixed
            quarter.Add("LXCH",9);
            quarter.Add("INCR", 10);
            quarter.Add("ADS", 11);
            quarter.Add("DXCH", 21);
            quarter.Add("TS", 22);
            quarter.Add("XCH", 23);
            extrac.Add("DV", 1);
            extrac.Add("MSU", 2);
            extrac.Add("DCA", 3);
            extrac.Add("DCS", 4);
            extrac.Add("INDEX", 5);
            extrac.Add("SU", 6);
            extrac.Add("MP", 7);
            //extraq.Add("BZF", 5); //TODO SAME as TCF
            extraq.Add("QXCH", 9);
            extraq.Add("AUG", 10);
            extraq.Add("DIM", 11);
            //extraq.Add("BZMF", 25); //TODO same as TCF
            IACode.Add("XXALQ", 0);
            IACode.Add("XLQ", 1);
            IACode.Add("RETURN", 2);
            IACode.Add("RELINT", 3);
            IACode.Add("INHINT", 4);
            IACode.Add("EXTEND", 6);
            IACode.Add("DDOUBL", 8193);
            IACode.Add("ZL", 9223);
            IACode.Add("COM", 16384);
            IACode.Add("DTCF", 21509);
            IACode.Add("DTCB", 21510);
            IACode.Add("OVSK", 22528);
            IACode.Add("TCAA", 22533);
            IACode.Add("DOUBLE", 24576);
            IACode.Add("ZQ", 9223);
            IACode.Add("DCOM", 16385);
            IACode.Add("SQUARE", 28672);
            IACode.Add("NOOP", 7);
            IACode.Add("RESUME", 20495);
            IOCode.Add("READ", 0);
            IOCode.Add("WRITE", 0);
            IOCode.Add("RAND", 0);
            IOCode.Add("WAND", 0);
            IOCode.Add("ROR", 0);
            IOCode.Add("WOR", 0);
            IOCode.Add("RXOR", 0);
            IOCode.Add("WXOR", 0);
        }
    }

    public class AGC_Compiler
    {
        BANK current_bank;
        ushort FB, EB;
        int FEB;
        StreamReader AGC_Code;
        String[] Cp_File;
        FileInfo AGC_C;
        FileStream AGC_Bit;
        String AGC_Code_File;
        String AGC_Bit_File;
        BANK B;
        int max_pass = 5;
        int bank_index = 0;
        int[] bank_count;
        Dictionary<String, int> labels = new Dictionary<String, int>();
        fvDict fix = new fvDict();
        bool bank_changed;
        int error = 0;
        int pass_count = 0;
        int lerror = -6;
        int FC_count = 0;
        string bank_type = "FB";

        public AGC_Compiler(String FInput, String FOutput)
        {        
            AGC_Code_File = FInput;
            AGC_Bit_File = FOutput;
            bank_count = new int[43];
            for (int i = 0; i < 43; i++ )
            { bank_count[i] = 0; }
                Cp_File = File.ReadAllLines(AGC_Code_File);
            FB = 0; EB = 0;
            FEB = 0; lerror = -6;
            B = new BANK(false, FB, FEB, AGC_Bit_File);
            B.compiling = true;
            bank_changed = false;
            }

        public int compile()
        {
            while (lerror == -6 && pass_count < max_pass) //until all labels are resolved
            {
                FC_count = 0;
                FB = 0; EB = 0; FEB = 0;
                bank_changed = true;
                for (int i = 0; i < 43; i++)
                { bank_count[i] = 0; }
                bank_index = 0;
                process_line(0);
                pass_count++;
                Console.WriteLine("Compile pass : {0}", pass_count);
            }
            if(pass_count == max_pass)
            { return -6; }
            output_labels();
            FB = 0; EB = 0; FEB = 0;
            bank_changed = true;
            for (int i = 0; i < 43; i++)
            { bank_count[i] = 0; }
            bank_index = 0;
            process_line(1);
            return error;
        }

        private void output_labels()
        {if(File.Exists("Labels_"+AGC_Code_File))
        { File.Delete("Labels_" + AGC_Code_File); }
        FileStream fs = File.Create("Labels_" + AGC_Code_File);
          fs.Close();
          fs.Dispose();
          StreamWriter sw = new StreamWriter("Labels_" + AGC_Code_File, true);
        string output;
        foreach (KeyValuePair<string, int> kvp in labels)
        {
            output = String.Format("Label : {0} - Adress : 0x{1:X} \n", kvp.Key, kvp.Value);
            sw.Write(output);
        }
        sw.Close();
        }

        public int process_line(int mode)
        {
            String current;
            char[] sep = new char[] { '\t' };
            for (int i = 0; i < Cp_File.Length; i++)
            {
                if ((current = Cp_File[i]) != null)
                {
                    if (bank_changed)
                    {
                        switch(bank_type)
                        {
                            case "FB": B = new BANK(false, FB, FEB, AGC_Bit_File); break;
                            case "EB": B = new BANK(true, EB, 0, AGC_Bit_File); break;
                        }
                        B.compiling = true;
                        bank_changed = false;
                    }
                    String[] items = current.Split(sep, StringSplitOptions.None);
                    switch (mode)
                    {
                        case 0: if (items[0] != "") { error = resolve_labels(items);}
                            else
                            {
                                try
                                {
                                    switch (items[1])
                                    {
                                        case "BANK": switch_bank(items); break;
                                        case "EBANK": switch_bank(items); break;
                                        case "SETLOC": bank_index = (ushort)Int16.Parse(items[2], System.Globalization.NumberStyles.HexNumber); break;
                                        case "2FCADR": lerror = toFCADR(items); break;
                                        case "ERASE": bank_index++; break;
                                        default: bank_index++; break;
                                    }
                                }
                                catch { }
                            } break;
                        case 1: try
                            {
                                if (items[1] != "")
                                {
                                    error = resolve_opcode(items);
                                }
                            }
                            catch { }
                            break;
                    }
                }
                else { error = -4; } //EOF
                if(error != 0 && lerror != -6)
                { return error; }
            }
            return error;
        }

        public int resolve_labels(String[] items)
        {
                int adress = B.get_ba()/16 + bank_index;
                int val = 0;
            if(labels.TryGetValue(items[0], out val))
            {
                error = -5; //labels already exist
            }
            if (error != -5)
            {
                labels.Add(items[0], adress);
            }
                switch (items[1])
                {
                    case "=": 
                        if(error!=-5)
                        { B.set_sword((ushort)bank_index, new sWord(ResolveOperand(items[2])));
                        B.write_bank();} break;
                    case "2FCADR": lerror = toFCADR(items);
                        return lerror;
                }
                bank_index += 1;
            return 0;
        }

        public int resolve_opcode(String[] items)
        { ushort opcode = 0;
          ushort adress = 0;
          if (fix.opcode.TryGetValue(items[1], out opcode)) { opcode *= 4096; }
          else if (fix.quarter.TryGetValue(items[1], out opcode)) { opcode *= 1024; }
          else if (fix.extrac.TryGetValue(items[1], out opcode)) { opcode *= 4096; }
          else if (fix.extraq.TryGetValue(items[1], out opcode)) { opcode *= 1024; }
          else if (fix.IACode.TryGetValue(items[1], out opcode))
          {
              if (opcode == 7)
              {
                  if (B.isErasable()) { B.set_sword((ushort)bank_index, new sWord(0x3000, true)); }
                  else { B.set_sword((ushort)bank_index, new sWord((ushort)(0x1001+bank_index), true)); }
              }
          }
          else
          {
              switch (items[1])
              {
                  case "SETLOC": error = SETLOC(items); return 0;
                  case "ERASE": bank_index++; break;
                  case "BANK": switch_bank(items); return 0;
                  case "EBANK": switch_bank(items); return 0;
                  case "=": bank_index++; return 0;
                  case "2FCADR": bank_index += 2; return 0;
                  default: bank_index++; return -1;
              }
          }
          adress = ResolveOperand(items[2]);
          sWord ad = new sWord((ushort)(opcode + adress), true);
          B.set_sword((ushort)bank_index, ad);
          B.write_bank();
          bank_index += 1;
        return 0;}

        public int SETLOC(string[] item)
        {
            int val = 0;
            int keyval;
            try
            {
                val = Int16.Parse(item[2], System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                if (labels.TryGetValue(item[2], out keyval))
                {val = keyval;                }
                else { return -6; //unknown label
                }
            }
            if(B.isErasable())
            {   if(val <= 0xFF)
                {bank_index = val;}
                else{return -7;} //index out of range
            }
            else{
                if(val <= 0x3FF)
                {bank_index = val;}
                else{return -7;} //index out of range
                }
            return 0;
        }

        public int toFCADR(string[] item)
        {
            sWord adr = null;
            try { adr = new sWord((ushort)Int16.Parse(item[2], System.Globalization.NumberStyles.HexNumber), true);}
            catch
            {
                int val = 0;
                if (labels.TryGetValue(item[2], out val))
                {
                    adr = new sWord((ushort)val, true);                  
                }
                else
                {
                    bank_index += 2;
                    FC_count += 1;
                    return -6;}
            }
            int tad = adr.getVal(10, 14);
            if (tad < 4)
            {
                B.set_sword((ushort)bank_index, new sWord((ushort)(adr.getVal(10, 14)), true));
            }
            else
            {
                B.set_sword((ushort)bank_index, new sWord((ushort)(adr.getVal(10, 14) - 4), true));
            }            
                bank_index++;
                B.set_sword((ushort)bank_index, new sWord(adr.getVal(0, 9), true));
                bank_index++;
                B.write_bank();
                if(FC_count > 0)
                { return -6; }
                return 0;            
        }

        public ushort ResolveOperand(string item)
        {
            ushort adress = 0;
            try { adress = (ushort)Int16.Parse(item, System.Globalization.NumberStyles.HexNumber); }
            catch
            {
                int val = 0;
                if (labels.TryGetValue(item, out val))
                {
                    sWord adr = new sWord();
                    if (val >= 0x1000)
                    {
                        adress = (ushort)(val - 0x1000 + 0x400);
                    }
                    else
                    {
                        if (val >= 0x400 && val <= 0x7FF)
                        {
                            adr = new sWord((ushort)val);
                            adress = (ushort)(adr.getVal(0, 7) + 0x300);
                        }
                        else { adress = (ushort)val; }
                    }
                }
                else if (fix.registers.TryGetValue(item, out val))
                {
                    adress = (ushort)val;
                }
                else
                {
                    return 1;
                }
            }
            return adress;
        }

        public void switch_bank(string[] items)
        {
            save_index();
            B.write_bank();
            switch (items[1])
            {
                case "BANK": FB = (ushort)int.Parse(items[2], System.Globalization.NumberStyles.Integer);
                    if (FB > 32)
                    { FEB = 1;}
                    else { FEB = 0; }
                    bank_index = bank_count[FB];
                    bank_type = "FB";
                    break;
                case "EBANK": EB = (ushort)int.Parse(items[2], System.Globalization.NumberStyles.Integer);
                    bank_index = bank_count[EB];
                    bank_type = "EB";
                    break;
            }
            bank_changed = true;

        }

        public void save_index()
        {
           if(B.isErasable())
           {
               bank_count[B.getId()] = bank_index;
           }
           else
           {
               if(B.getFEB() != 1)
               { bank_count[B.getId() + 8] = bank_index; }
               else { bank_count[B.getId() + 16] = bank_index; }
           }
        }
    }
}
