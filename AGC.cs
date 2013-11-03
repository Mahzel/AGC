using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AGC_SUPPORT;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        BANK wB;
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
        bool fFixed;
        bool tEr;
        ushort tId;
        int tFEB;

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
        }

        /// <summary>
        /// start (run) a previous AGC intialized (powered up)
        /// </summary>
        public void start()
        {
            if (!running) {
                clock.c_start ();
                running = true;
                PB = new BANK (false, 0, 0, AGC_File); //load the current pgrogram bank - at launch : FB0
                wB = new BANK (true, 0, 0, AGC_File); //load a default EB 0
                fFixed = false;
                Console.WriteLine ("AGC Started");
                e_mem = build_adress_reg (PB.get_word (RegBank.get_word (5).getHex ()));
                RegBank.set_sword (11, PB.get_word (RegBank.get_word (5).getHex ()).getVal (12, 15)); //set SQ reg to opcode value
                QC = PB.get_word (RegBank.get_word (5).getHex ()).getVal (10, 11);
                RegBank.set_sword (5, (ushort)(RegBank.get_word (5).getHex () + 1));
                RegBank.write_bank ();
                MCT ();
            } else {
                Console.WriteLine ("AGC Halted.");
            }
           
        }

        public void read_chan(int index)
        {
                    Console.WriteLine("DSKY Wrote Index : {0} - Value : {1}",index, chan.get_chan(index));
        }

        public void write_chan(int index, ushort value)
        {
            chan.set_chan(this, index, value);
        }

        private void MCT()
        {
            while (running)
            {
                int cycle_count = 0;
                clock.c_start();
                exec_opc(false);
                if((RegBank.get_word(3).getHex() != wB.getId()) && wB.isErasable())
                { switch_bank(true);}
                if((RegBank.get_word(4).getHex() != wB.getId()) && !wB.isErasable())
                { switch_bank(false);}
                e_mem = build_adress_reg(PB.get_word(RegBank.get_word(5).getHex()));
                RegBank.set_sword(11, PB.get_word(RegBank.get_word(5).getHex()).getVal(12, 15)); //set SQ reg to opcode value
                QC = PB.get_word(RegBank.get_word(5).getHex()).getVal(10, 11);
                RegBank.set_sword(5, (ushort)(RegBank.get_word(5).getHex() + 1)); //Increment Z to next instruction
                RegBank.write_bank();
                while (cycle_count < 1)
                {
                    cycle_count += clock.get_cycle();
                }
            }
            Console.WriteLine("AGC halted");
        }

        public void switch_bank(bool erasable)
        {
            if (erasable)
            {
                wB.write_bank();
                wB = new BANK(true,RegBank.get_word(3).getHex(),0,AGC_File);
            }
            else 
            {
                wB = new BANK(false, RegBank.get_word(4).getHex(), FEB, AGC_File);
            }
            
        }
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
            if (adress.getVal(11,11) == 0 && adress.getVal(10, 10) == 0)
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
            RegBank.set_sword(12, S); //set S reg to operand value
            return erasable;
        }
        /// <summary>
        /// exec opcode routine
        /// disassamble word, find opcode then call function
        /// </summary>
        /// <param name="extra">Extracode instruction</param>
        public void exec_opc(Boolean extra)
        {
            int S = (int)RegBank.get_word(12).getHex();
            
            if (!extra)
            {
                switch (RegBank.get_word(11).getHex())
                {
                    case 0:
                        if(fFixed)
                        { S=fFixed_switch(S); }
                        Console.WriteLine("TC {0:X4}", S);
                        break;
                    case 1:
                        if (fFixed)
                        {S= fFixed_switch(S); }
                        if (QC == 0)
                        {
                            Console.WriteLine("CCS {0:X4}", S);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("TCF {0:X4}", S);
                            break;
                        }
                    case 2:
                        if (fFixed)
                        { S=fFixed_switch(S - (QC * 1024)); }
                        switch (QC)
                        {
                            case 0:
                                Console.WriteLine("DAS {0:X4}", S);
                                break;
                            case 1:
                                Console.WriteLine("LXCH {0:X4}", S);
                                break;
                            case 2:
                                Console.WriteLine("INCR {0:X4}", S);
                                INCR(S);
                                break;
                            case 3:
                                Console.WriteLine("ADS {0:X4}", S);
                                break;
                        }
                        break;
                    case 3:
                        if (fFixed)
                        { S=fFixed_switch(S); }
                        Console.WriteLine("CA {0:X4}", S);
                        CA(S);
                        break;
                    case 4:
                        if (fFixed)
                        { S=fFixed_switch(S); }
                        Console.WriteLine("CS {0:X4}", S);
                        break;
                    case 5:
                        if (fFixed)
                        { S=fFixed_switch(S - (QC * 1024)); }
                        switch (QC)
                        {
                            case 0:
                                Console.WriteLine("INDEX {0:X4}", S);
                                break;
                            case 1:
                                Console.WriteLine("DXCH {0:X4}", S);
                                break;
                            case 2:
                                Console.WriteLine("TS {0:X4}", S);
                                TS(S);
                                break;
                            case 3:
                                Console.WriteLine("XCH {0:X4}", S);
                                break;
                        }
                        break;
                    case 6:
                        if (fFixed)
                        { S=fFixed_switch(S); }
                        Console.WriteLine("AD {0:X4}", S);
                        AD(S);
                        break;
                    case 7:
                        if (fFixed)
                        { S=fFixed_switch(S); }
                        Console.WriteLine("MASK {0:X4}", S);
                        running = false;
                        break;
                }
            }
            else { } //TODO : EXTRACODES
            if(fFixed)
            { restore_fFixed(); }
        }

        public void restore_fFixed()
        {
            wB.write_bank();
            if(wB.isErasable() && (wB.getId() == 0))
            { RegBank = new BANK(true, 0, 0, AGC_File); }
            wB = new BANK(tEr, tId, tFEB, AGC_File);
            e_mem = tEr;
        }

        public int fFixed_switch(int adr)
        {
                tEr = wB.isErasable();
                tId = (ushort)wB.getId();
                tFEB = wB.getFEB();
                sWord s = new sWord((ushort)adr, true);
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

        public void AD(int adress)
        {
            ushort val_adr = 0;
            val_adr = wB.get_word((ushort)adress).getHex();    
            RegBank.set_sword(0, (ushort)(val_adr + RegBank.get_word(0).getHex()));
            RegBank.write_word(0);
        }

        public void TS(int adress)
        {
            if (e_mem)
            {
                wB.set_sword((ushort)adress, RegBank.get_word(0).getHex());
                wB.write_word((ushort)adress);
            }
        }

        public void INCR(int adress)
        {
            if (e_mem)
            {
                wB.set_sword((ushort)adress, (ushort)(wB.get_word((ushort)adress).getHex() + 1));
                wB.write_word((ushort)adress);
            }
        }

        public void CA(int adress)
        {
            RegBank.set_sword(0, 0);
            AD(adress);
        }


        public void ovfCor()
        { }
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
            agc.RegBank.set_sword(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_word(0));
            ushort actual = agc.RegBank.get_word(12).getHex();

            //assert
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void test_BuildAdress_FFixed()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort bit_adr = 0x801;
            ushort expected = 1;
            ushort exp_FB = 2;
            agc.RegBank.set_sword(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_word(0));
            ushort actual = agc.RegBank.get_word(12).getHex();
            ushort actual_FB = agc.RegBank.get_word(4).getHex();
            //assert
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(exp_FB, actual_FB);
        }
        [TestMethod]
        public void test_BuildAdress_EFixed()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort bit_adr = 0x101;
            ushort expected = 1;
            ushort exp_EB = 1;
            agc.RegBank.set_sword(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_word(0));
            ushort actual = agc.RegBank.get_word(12).getHex();
            ushort actual_EB = agc.RegBank.get_word(3).getHex();
            //assert
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(exp_EB, actual_EB);
        }
        [TestMethod]
        public void test_BuildAdress_ESwitch()
        {
            //Arrange
            AGC agc = new AGC("TestFile.agc", chan);
            ushort bit_adr = 0x301;
            ushort expected = 1;
            agc.RegBank.set_sword(0, bit_adr);

            //act
            agc.build_adress_reg(agc.RegBank.get_word(0));
            ushort actual = agc.RegBank.get_word(12).getHex();
            //assert
            Assert.AreEqual(expected, actual);
        }
    }

}
