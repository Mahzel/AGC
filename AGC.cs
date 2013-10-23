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
        BANK EB;
        String AGC_File;
        /// <summary>
        /// Quarter code holding variable
        /// </summary>
        ushort QC;
        ushort FEB;
        bool e_mem;
        Channels chan;
        int test_int;

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
                EB = new BANK (true, 0, 0, AGC_File); //load a default EB 0
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

        public void switch_work_bank(bool erasable)
        {
            if (erasable)
            {
                if (PB.isErasable() && (PB.getId() != RegBank.get_word(3).getHex()))
                {
                    PB.write_bank();
                    PB = new BANK(true, RegBank.get_word(3).getHex(), 0, AGC_File);
                }
                else if (!PB.isErasable())
                {
                    PB = new BANK(true, RegBank.get_word(3).getHex(), 0, AGC_File);
                }
            }
            else if (!PB.isErasable() && (PB.getId() != RegBank.get_word(4).getHex()))
            {
                PB = new BANK(false, RegBank.get_word(4).getHex(), 0, AGC_File);
            }
            else if (PB.isErasable())
            {
                PB.write_bank();
                PB = new BANK(false, RegBank.get_word(4).getHex(), 0, AGC_File);
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
            ushort EB = 0;
            ushort FB = 0;
            bool erasable = false;
            if (adress.getVal(11,11) == 0 && adress.getVal(10, 10) == 0)
            {
                if ((adress.getVal(9, 9) != 1) | (adress.getVal(8,8) != 1))
                { // not to be normally used
                    EB = (ushort)adress.getVal(8,9);
                    RegBank.set_sword(3, EB); //set EB to fixed-erasable bank computed value
                    S = adress.getVal(0, 7);
                }
                else
                {
                    S = (ushort)(adress.getVal(0, 9) - 0x300);
                }
                erasable = true;
            }
            else if (adress.getVal(11,11) == 1)
            {
                FB = (ushort)adress.getVal(10, 11);
                RegBank.set_sword(4, FB); //set FB reg to fxed-fixed bank computed index
                S = adress.getVal(0, 9);
                erasable = false;
            }
            else
            {
                S = (ushort)(adress.getVal(0, 11) - 0x400);
                //building offset -0x400 because bit 1 is nescessary in bit 11 in order for AGC to recognize a F-Switchable state
                erasable = false;
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
                        Console.WriteLine("TC {0:X4}", S);
                        break;
                    case 1:
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
                                break;
                            case 3:
                                Console.WriteLine("ADS {0:X4}", S);
                                break;
                        }
                        break;
                    case 3:
                        Console.WriteLine("CA {0:X4}", S);
                        break;
                    case 4:
                        Console.WriteLine("CS {0:X4}", S);
                        break;
                    case 5:
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
                                break;
                            case 3:
                                Console.WriteLine("XCH {0:X4}", S);
                                break;
                        }
                        break;
                    case 6:
                        Console.WriteLine("AD {0:X4}", S);
                        break;
                    case 7:
                        Console.WriteLine("MASK {0:X4}", S);
                        running = false;
                        break;
                }
            }
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
