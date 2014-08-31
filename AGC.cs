using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AGC_SUPPORT;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace nAGC
{
    /// <summary>
    /// placeholder for an AGC with basic registries
    /// </summary>
    public class AGC
    {
        public BANK RegBank;
        bool running;
        /// <summary>
        /// AGC clock
        /// </summary>
        CLOCK clock;
        /// <summary>
        /// BANK in use
        /// </summary>
        BANK PB;
        internal BANK wB;
        BANK tB;
        String AGC_File;
        /// <summary>
        /// Quarter code holding variable
        /// </summary>
        ushort QC;
        ushort FEB;
        bool e_mem;
        Channels chan;
        int test_int;
        internal bool fFixed;
        bool tEr;
        ushort tId;
        int tFEB;
        bool extra = false;
        bool debug = false;
        int index = 0;
        int PC=0; //Peripheral Code of IO channels.

        /// <summary>
        /// AGC builder placeholder
        /// </summary>
        public AGC(String file, Channels chan)
        {
            AGC_File = file;
            FEB = 0;
            RegBank = new BANK(true, 0, 0, AGC_File);
            running = false;
            clock = new CLOCK();
            PB = null;
            e_mem = false;
            this.chan = chan;
            chan.regDelegate(this, read_chan);
            PB = new BANK(false, 0, 0, AGC_File); //load the current pgrogram bank - at launch : FB0
            wB = new BANK(true, 0, 0, AGC_File); //load a default EB 0
            fFixed = false;
        }

        public void setDebug(bool deb)
        {
            this.debug = deb;
        }
        
        //Run functions
        /// <summary>
        /// start (run) a previous AGC intialized (powered up)
        /// </summary>
        public void start(bool step)
        {
            if (!running) {
                clock.c_start ();
                running = true;
                Console.WriteLine ("AGC Started");
                e_mem = build_adress_reg (PB.get_sword (0));
                RegBank.set_word (11, PB.get_sword (0).getVal (12, 15)); //set SQ reg to opcode value
                QC = PB.get_sword (0).getVal (10, 11);
                RegBank.set_word (5, (ushort)(1));
                RegBank.write_bank ();
                MainCycle(step);
            } else {
                if (debug) { chan.set_chan(this, 6, 1); }
            }
           
        }

        public bool isRunning()
        { return running; }

        private void MainCycle(bool step)
        {
            if (!step)
            {
                while (running)
                {
                    MCT();
                }
                if (debug) { chan.set_chan(this, 6, 1); }
                Console.WriteLine("AGC halted");
            }
            else
            {
            }
        }
        public void MCT()
        {
                int cycle_count = 0;
                clock.c_start();
                exec_opc();
                if(running == false)
                {
                    chan.set_chan(this, 6, 1);
                    return;
                }
                if ((RegBank.get_word(3) != wB.getId()) && wB.isErasable())
                { switch_bank(true); }
                if ((RegBank.get_word(4) != wB.getId()) && !wB.isErasable())
                { switch_bank(false); }
                e_mem = build_adress_reg(PB.get_sword(RegBank.get_word(5)));
                RegBank.set_word(11, PB.get_sword(RegBank.get_word(5)).getVal(12, 14)); //set SQ reg to opcode value
                QC = PB.get_sword(RegBank.get_word(5)).getVal(10, 11);
                RegBank.set_word(5, (ushort)(RegBank.get_word(5) + 1)); //Increment Z to next instruction
                RegBank.write_bank();
                while (cycle_count < 1)
                {
                    cycle_count += clock.get_cycle();
                }
                if (debug)
                {
                    chan.set_chan(this, 0, RegBank.get_word(0));
                    chan.set_chan(this, 1, RegBank.get_word(1));
                    chan.set_chan(this, 2, RegBank.get_word(2));
                    chan.set_chan(this, 3, RegBank.get_word(3));
                    chan.set_chan(this, 4, RegBank.get_word(4));
                    chan.set_chan(this, 5, RegBank.get_word(5));
                }
        }

        public void Halt()
        {
            running = false;
            Console.WriteLine("AGC halted");
            if (debug) { chan.set_chan(this, 6, 1); }
        }
        
        //Bank management
        public void switch_bank(bool erasable)
        {
            if (erasable)
            {
                wB.write_bank();
                wB = new BANK(true,RegBank.get_word(3),0,AGC_File);
            }
            else 
            {
                wB = new BANK(false, RegBank.get_word(4), FEB, AGC_File);
            }
            
        }
        public void restore_fFixed()
        {
            wB.write_bank();
            if (wB.isErasable() && (wB.getId() == 0))
            { RegBank = new BANK(true, 0, 0, AGC_File); }
            wB = new BANK(tEr, tId, tFEB, AGC_File);
            e_mem = tEr;
        }
        public int fFixed_switch(int adr)
        {
            tEr = wB.isErasable();
            tId = (ushort)wB.getId();
            tFEB = wB.getFEB();
            sWord s = new sWord((ushort)adr);
            if (s.getVal(10, 11) == 0)
            {
                if ((ushort)s.getVal(8, 9) == 0)
                { wB = RegBank; }
                else
                {
                    wB = new BANK(true, (ushort)s.getVal(8, 9), 0, AGC_File);
                }
                adr = s.getVal(0, 7);
                e_mem = true;
            }
            else
            {
                wB = new BANK(false, (ushort)s.getVal(10, 11), 0, AGC_File);
                adr = s.getVal(0, 11);
                e_mem = false;
            }
            return adr;
        }
        
        //Execution
        /// <summary>
        /// <para>edit adress registry (FB/EB/FEB/S) based on the bits 12-1 of the current instruction</para>
        /// <para>detect usage of FB/EB register</para>
        /// <para>if not needed, will set them accordingly to the bank to load</para>
        /// </summary>
        /// <param name="adress">byte[] : 16b adress word</param>
        /// <returns>bool : type (true : erasable, false : fixed)</returns>
        public bool build_adress_reg(sWord adress)
        {
            ushort S = 0;

            bool erasable = false;
            if (extra && adress.getVal(12, 14) == 0)//IO Code detection
            {
                S = adress.getVal(0, 8);
                PC = adress.getVal(9, 11);
                erasable = true;
                fFixed = false;
            }
            else if (adress.getVal(11,11) == 0 && adress.getVal(10, 10) == 0)
            {
                if ((adress.getVal(9, 9) != 1) | (adress.getVal(8,8) != 1))
                {
                    S = adress.getVal(0, 11);
                    fFixed = true;
                }
                else
                {
                    S = (ushort)(adress.getVal(0, 9) - 0x300);
                    fFixed = false;
                }
                erasable = true;
            }
            else if (adress.getVal(11,11) == 1)
            {
                    S = adress.getVal(0, 11);
                    erasable = false;
                    fFixed = true;
            }
            else
            {
                S = (ushort)(adress.getVal(0, 11) - 0x400);
                //building offset -0x400 because bit 1 is nescessary in bit 11 in order for AGC to recognize a F-Switchable state
                erasable = false;
                fFixed = false;
            }
            RegBank.set_word(12, S); //set S reg to operand value
            return erasable;
        }
        /// <summary>
        /// exec opcode routine
        /// disassamble word, find opcode then call function
        /// </summary>
        /// <param name="extra">Extracode instruction</param>
        public void exec_opc()
        {       
            if (!extra)
            {
                stOpcode(RegBank.get_sword(12));
            }
            else { extraCodes(RegBank.get_sword(12)); }
            if(fFixed)
            { restore_fFixed(); }
        }
        private void stOpcode(sWord S)
        {
            int val = 0;
            if (fFixed)
            { val = fFixed_switch(S.getInt()); }
            else { val = S.getInt(); }
            switch (RegBank.get_word(11))
            {
                case 0:
                    if (val > 6)
                    {
                        TC(val + index);
                        Console.WriteLine("TC {0:X4}", val + index);
                    }
                    else
                    {
                        switch (val)
                        {
                            case 0:
                                Console.WriteLine("XXALQ");
                                break;
                            case 1:
                                Console.WriteLine("XLQ");
                                break;
                            case 2:
                                Console.WriteLine("RETURN");
                                break;
                            case 3:
                                Console.WriteLine("RELINT");
                                break;
                            case 4:
                                Console.WriteLine("INHINT");
                                break;
                            case 5:
                                TC(val + index);
                                Console.WriteLine("TC {0:X4}", val + index);
                                break;
                            case 6:
                                extra = true;
                                Console.WriteLine("EXTEND");
                                break;
                        }
                    }
                    index = 0;
                    break;
                case 1:
                    if (QC == 0)
                    {
                        Console.WriteLine("CCS {0:X4}", val+index);
                        index = 0;
                        break;
                    }
                    else
                    {
                        Console.WriteLine("TCF {0:X4}", val + index);
                        index = 0;
                        break;
                    }
                case 2:
                    switch (QC)
                    {
                        case 0:
                            if (val == 0)
                            {
                                Console.WriteLine("DDOUBL");
                                break;
                            }
                                Console.WriteLine("DAS {0:X4}", val + index);
                                break;
                        case 1:
                            if(val==6)
                            {
                                Console.WriteLine("ZL");
                                break;
                            }
                            Console.WriteLine("LXCH {0:X4}", val + index);
                            break;
                        case 2:
                            Console.WriteLine("INCR {0:X4}", val + index);
                            INCR(val + index);
                            break;
                        case 3:
                            Console.WriteLine("ADS {0:X4}", val + index);
                            break;
                    }
                    index = 0;
                    break;
                case 3:
                    Console.WriteLine("CA {0:X4}", val + index);
                    CA(val + index);
                    index = 0;
                    break;
                case 4:
                    if(val==0)
                    {
                        Console.WriteLine("COM");
                        break;
                    }
                    Console.WriteLine("CS {0:X4}", val + index);
                    index = 0;
                    break;
                case 5:
                    switch (QC)
                    {
                        case 0:
                            if(val==17)
                            {
                                Console.WriteLine("RESUME");
                            }
                            Console.WriteLine("INDEX {0:X4}", val + index);
                            index = wB.get_word((ushort)(val + index));
                            break;
                        case 1:
                            switch(val)
                            {
                                case 4:
                                    Console.WriteLine("DTCF");
                                    break;
                                case 5:
                                    Console.WriteLine("DTCB");
                                    break;
                                default:
                                    Console.WriteLine("DXCH {0:X4}", val + index);
                                    break;
                            }
                            index = 0;
                            break;
                        case 2:
                            switch (val)
                            {
                                case 4:
                                    Console.WriteLine("OVSK");
                                    break;
                                case 5:
                                    Console.WriteLine("TCAA");
                                    break;
                                default:
                                   Console.WriteLine("TS {0:X4}", val + index);
                                    TS(val+index);
                                    break;
                            }
                            Console.WriteLine("TS {0:X4}", val + index);
                            TS(val+index);
                            index = 0;
                            break;
                        case 3:
                            Console.WriteLine("XCH {0:X4}", val + index);
                            index = 0;
                            break;
                    }
                    break;
                case 6:
                    if (val != 0)
                    {
                        Console.WriteLine("AD {0:X4}", val + index);
                        AD(val + index);
                    }
                    else
                    {
                        Console.WriteLine("DOUBLE");
                    }
                    index = 0;
                    break;
                case 7:
                    if (fFixed)
                    { val = fFixed_switch(S.getInt()); }
                    else { val = S.getInt(); }
                    Console.WriteLine("MASK {0:X4}", val + index);
                    running = false;
                    index = 0;
                    break;
            }
        }

        private void extraCodes(sWord S)
        {
            int val = 0;
            if (fFixed)
            { val = fFixed_switch(S.getInt()); }
            else { val = S.getInt(); }
            switch (RegBank.get_word(11))
            {
                case 0:
                    val = S.getVal(0, 8);
                    switch(PC)
                    {
                        case 0:
                            Console.WriteLine("READ {0:X4}", val+index);
                            break;
                        case 1:
                            Console.WriteLine("WRITE {0:X4}", val+index);
                            write_chan(val, RegBank.get_word(0));
                            break;
                        case 2:
                            Console.WriteLine("RAND {0:X4}", val+index);
                            break;
                        case 3:
                            Console.WriteLine("WAND {0:X4}", val+index);
                            break;
                        case 4:
                            Console.WriteLine("ROR {0:X4}", val+index);
                            break;
                        case 5:
                            Console.WriteLine("WOR {0:X4}", val+index);
                            break;
                        case 6:
                            Console.WriteLine("RXOR {0:X4}", val+index);
                            break;
                        case 7:
                            Console.WriteLine("WXOR {0:X4}", val+index);
                            break;
                    }
                    index = 0;
                    extra = false;
                    break;
                case 1:
                    if(QC==0)
                    {
                        Console.WriteLine("DV {0:X4}", val + index);
                    }
                    else
                    {
                        Console.WriteLine("BZF {0:X4}", val + index);
                    }
                    index = 0;
                    extra = false;
                    break;
                case 2:
                    switch(QC)
                    {
                        case 1:
                            Console.WriteLine("MSU {0:X4}", val + index);
                            break;
                        case 2:
                            if (val == 7)
                            {
                                Console.WriteLine("ZQ");
                            }
                            else
                            {
                                Console.WriteLine("QXCH {0:X4}", val + index);
                            }
                            break;
                        case 3:
                            Console.WriteLine("AUG {0:X4}", val + index);
                            break;
                        case 4:
                            Console.WriteLine("DIM {0:X4}", val + index);
                            break;
                    }
                    index = 0;
                    extra = false;
                    break;
                case 3:
                    if (val == 0)
                    {
                        Console.WriteLine("DCOM");
                    }
                    else
                    {
                        Console.WriteLine("DCA {0:X4}", val + index);
                    }
                    index = 0;
                    extra = false;
                    break;
                case 4:
                    Console.WriteLine("DCS {0:X4}", val + index);
                    index = 0;
                    extra = false;
                    break;
                case 5:
                    Console.WriteLine("INDEX {0:X4}", val + index);
                    index = wB.get_word((ushort)(val + index));
                    extra = true;
                    break;
                case 6:
                    if(QC==0)
                    { Console.WriteLine("SU {0:X4}", val + index); }
                    else { Console.WriteLine("BZMF {0:X4}", val + index); }
                    index = 0;
                    extra = false;
                    break;
                case 7:
                    if (val == 0)
                    {
                        Console.WriteLine("SQUARE");
                    }
                    else
                    {
                        Console.WriteLine("MP {0:X4}", val + index);
                    }
                    index = 0;
                    extra = false;
                    break;
            }
        }
        
        //OPCODES Function
        public void AD(int adress)
        {
            ushort val_adr = 0;
            val_adr = wB.get_word((ushort)adress);    
            RegBank.set_word(0, (ushort)(val_adr + RegBank.get_word(0)));
            RegBank.write_word(0);
        }
        public void ADS(int adress)
        {
            ushort val_adr = 0;
            val_adr = wB.get_word((ushort)adress);    
            RegBank.set_word(0, (ushort)(val_adr + RegBank.get_word(0)));
            RegBank.write_word(0);
            RegBank.set_word((ushort)adress, (ushort)RegBank.get_word(0));
            RegBank.write_word((ushort)adress);
        }
        public void DAS(int adress)
        {
            ushort val_adr1 = wB.get_word((ushort)adress);
            ushort val_adr2 = wB.get_word((ushort)(adress+1));   
            RegBank.set_word(0, (ushort)(val_adr1 + RegBank.get_word(0)));
            RegBank.set_word(1, (ushort)(val_adr2+ RegBank.get_word(1)));
            RegBank.write_word(0);
            RegBank.write_word(1);
            RegBank.set_word((ushort)adress, (ushort)RegBank.get_word(0));
            RegBank.set_word((ushort)(adress + 1), (ushort)RegBank.get_word(1));
            RegBank.write_word((ushort)adress);
            RegBank.write_word((ushort)(adress + 1));
        }
        public void SU(int adress)
        {
            ushort val_adr = 0;
            val_adr = wB.get_word((ushort)adress);    
            RegBank.set_word(0, (ushort)(val_adr - RegBank.get_word(0)));
            RegBank.write_word(0);
        }
        public void TS(int adress)
        {
            if (e_mem)
            {
                wB.set_word((ushort)adress, RegBank.get_word(0));
                wB.write_word((ushort)adress);
            }
        }
        public void CCS(int adress)
        {
            CA(adress);
            if(wB.get_word((ushort)adress)>0)
            {}
            else if(wB.get_word((ushort)adress)==0)
            {}
            else if(wB.get_word((ushort)adress)>16384)
            {sWord wd = new sWord(wB.get_word((ushort)(adress)));
                RegBank.set_word(0,1);}
            else{RegBank.set_word(0,0);
            }
        }
        public void INCR(int adress)
        {
            if (e_mem)
            {
                wB.set_word((ushort)adress, (ushort)(wB.get_word((ushort)adress) + 1));
                wB.write_word((ushort)adress);
            }
        }
        public void CA(int adress)
        {
            RegBank.set_word(0, 0);
            AD(adress);
        }
        public void TC(int adress)
        {
            RegBank.set_word(5, (ushort)(adress)); //Z will be incremented to correct instruction whith the MCT refresh cycle
        }
        public void AUG(int adress)
        {
            if(e_mem)
            {
                ushort wd = wB.get_word((ushort)adress);
                if(wd>=0)
                { wd += 1; }
                else { wd -= 1; }
                wB.set_word((ushort)adress, wd);
                wB.write_word((ushort)adress);
            }
        }
        public void DIM(int adress)
        {
            if (e_mem)
            {
                ushort wd = wB.get_word((ushort)adress);
                if (wd <= 0)
                { wd += 1; }
                else { wd -= 1; }
                wB.set_word((ushort)adress, wd);
                wB.write_word((ushort)adress);
            }
        }

        //Miscallaneous operations
        public void ovfCor()
        { }
        public void read_chan(int index)
        {
            Console.WriteLine("DSKY Wrote Index : {0} - Value : {1}", index, chan.get_chan(index));
        }
        public void write_chan(int index, ushort value)
        {
            chan.set_chan(this, index, value);
        }
    }

    [TestClass]
    public class AGC_Test
    {
        Channels chan = new Channels();
        [TestMethod]
        public void test_BuildAdress_FSwitch()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort bit_adr = 0x401;
            ushort expected = 1;
            agc.RegBank.set_word(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_sword(0));
            ushort actual = agc.RegBank.get_word(12);

            //assert
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void test_BuildAdress_FFixed()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort bit_adr = 0x801;
            ushort expected = 0x801;
            agc.RegBank.set_word(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_sword(0));
            ushort actual = agc.RegBank.get_word(12);
            ushort actual_FB = agc.RegBank.get_word(4);
            
            //assert
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(true, agc.fFixed);
        }
        [TestMethod]
        public void test_BuildAdress_EFixed()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort bit_adr = 0x101;
            ushort expected = 0x101;
            agc.RegBank.set_word(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_sword(0));
            ushort actual = agc.RegBank.get_word(12);
            ushort actual_EB = agc.RegBank.get_word(3);
            
            //assert
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(true, agc.fFixed);
        }
        [TestMethod]
        public void test_BuildAdress_ESwitch()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort bit_adr = 0x301;
            ushort expected = 1;
            agc.RegBank.set_word(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_sword(0));
            ushort actual = agc.RegBank.get_word(12);
            
            //assert
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void test_fFixed_switch()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort adr = 0x801;
            int expected_bank = 2;
            
            //act
            agc.fFixed_switch(adr);
            int bid = agc.wB.getId();
            bool fix = agc.wB.isErasable();

            //assert
            Assert.AreEqual(expected_bank, bid);
            Assert.AreEqual(fix, false);
        }

        [TestMethod]
        public void test_eFixedSwitch()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort adr = 0x101;
            int expected_bank = 1;

            //act
            agc.fFixed_switch(adr);
            int bid = agc.wB.getId();
            bool fix = agc.wB.isErasable();

            //assert
            Assert.AreEqual(expected_bank, bid);
            Assert.AreEqual(fix, true);
        }
    }

}
