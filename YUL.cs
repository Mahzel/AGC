using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AGC_SUPPORT;

namespace nYUL
{
    /// <summary>
    /// Compiler for the AGC
    /// TODO : Implement interpreted code
    /// </summary>
    public class YUL
    {
        ushort FB, EB;
        int FEB;
        String[] Cp_File;
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

        /// <summary>
        /// Constructor for the compiler
        /// </summary>
        /// <param name="FInput">The AGC Code file to read from</param>
        /// <param name="FOutput">The AGC Bin file to write to</param>
        public YUL(String FInput, String FOutput)
        {
            AGC_Code_File = FInput;
            AGC_Bit_File = FOutput;
            if (File.Exists(AGC_Bit_File))
            {
                File.Delete(AGC_Bit_File);
            }
            AGC_Bit = File.Create("AGC_Bin.bin");
            for (int j = 0; j < 655360; j++)
            {
                AGC_Bit.Write(new byte[] { 0 }, 0, 1);
            }
            AGC_Bit.Close();
            AGC_Bit.Dispose();
            Console.WriteLine("Empty bin file created");
            bank_count = new int[44];
            for (int i = 0; i < 44; i++)
            {
                bank_count[i] = 0;
            }
            Cp_File = File.ReadAllLines(AGC_Code_File);
            FB = 0;
            EB = 0;
            FEB = 0;
            lerror = -6;
            B = new BANK(false, FB, FEB, AGC_Bit_File);
            B.compiling = true;
            bank_changed = false;
        }

        /// <summary>
        /// The compilation routine process lines in mode 0 (resolve labels) then in mode 1 (resolve opcodes) and create the summary output file.
        /// A maximum of 5 passes is granted to process labels.
        /// </summary>
        /// <returns>Error index</returns>
        public int compile()
        {
            while (lerror == -6 && pass_count < max_pass)
            { //until all labels are resolved
                FC_count = 0;
                FB = 0;
                EB = 0;
                FEB = 0;
                bank_changed = true;
                for (int i = 0; i < 44; i++)
                {
                    bank_count[i] = 0;
                }
                bank_index = 0;
                process_line(0);
                pass_count++;
                Console.WriteLine("Compile pass : {0}", pass_count);
            }
            if (pass_count == max_pass)
            {
                return -6;
            }
            FB = 0;
            EB = 0;
            FEB = 0;
            bank_changed = true;
            for (int i = 0; i < 43; i++)
            {
                bank_count[i] = 0;
            }
            bank_index = 0;
            process_line(1);
            save_index();
            output_labels();
            return error;
        }

        /// <summary>
        /// Print labels list and memory status to "Labels_"+AGC_Code_File
        /// </summary>
        private void output_labels()
        {
            if (File.Exists("Labels_" + AGC_Code_File))
            {
                File.Delete("Labels_" + AGC_Code_File);
            }
            FileStream fs = File.Create("Labels_" + AGC_Code_File);
            fs.Close();
            fs.Dispose();
            StreamWriter sw = new StreamWriter("Labels_" + AGC_Code_File, true);
            string output;
            sw.Write("=============================\nLabels list & adress : \n=============================\n");
            foreach (KeyValuePair<string, int> kvp in labels)
            {
                output = String.Format("Label : {0} - Adress : 0x{1:X4} \n", kvp.Key, kvp.Value);
                sw.Write(output);
            }
            sw.Write("\n=============================\nMemory usage : \n=============================\n");
            for (int i = 0; i <= 7; i++)
            {
                output = String.Format("EBank : {0} - {1} word(s) used. \n", i, bank_count[i]);
                sw.Write(output);
            }
            sw.Write("=============================\n");
            for (int i = 0; i <= 31; i++)
            {
                output = String.Format("FBank : {0} - {1} word(s) used. \n", i, bank_count[i + 8]);
                sw.Write(output);
            }
            sw.Write("=============================\n");
            for (int i = 32; i <= 35; i++)
            {
                output = String.Format("SuperBank : {0} - {1} word(s) used. \n", i, bank_count[i + 8]);
                sw.Write(output);
            }
            sw.Close();
        }

        /// <summary>
        /// Read the AGC Code file and process lines
        /// </summary>
        /// <param name="mode">0 : process labels - 1: process opcodes</param>
        /// <returns>error index</returns>
        private int process_line(int mode)
        {
            String current;
            char[] sep = new char[] { '\t' };
            for (int i = 0; i < Cp_File.Length; i++)
            {
                if ((current = Cp_File[i]) != null)
                {
                    if (bank_changed)
                    {
                        switch (bank_type)
                        {
                            case "FB":
                                B = new BANK(false, FB, FEB, AGC_Bit_File);
                                break;
                            case "EB":
                                B = new BANK(true, EB, 0, AGC_Bit_File);
                                break;
                        }
                        B.compiling = true;
                        bank_changed = false;
                    }
                    String[] items = current.Split(sep, StringSplitOptions.None);
                    switch (mode)
                    {
                        case 0:
                            if (items[0] != "")
                            {
                                error = resolve_labels(items);
                            }
                            else
                            {
                                try
                                {
                                    switch (items[1])
                                    {
                                        case "BANK":
                                            switch_bank(items);
                                            break;
                                        case "EBANK":
                                            switch_bank(items);
                                            break;
                                        case "SETLOC":
                                            bank_index = (ushort)Int16.Parse(items[2], System.Globalization.NumberStyles.HexNumber);
                                            break;
                                        case "2FCADR":
                                            lerror = toFCADR(items);
                                            break;
                                        case "ERASE":
                                            B.set_sword((ushort)bank_index, (ushort)0);
                                            B.write_bank();
                                            bank_index++;
                                            break;
                                        default:
                                            B.set_sword((ushort)bank_index, (ushort)0);
                                            B.write_bank();
                                            bank_index++;
                                            break;
                                    }
                                }
                                catch
                                {
                                }
                            }
                            break;
                        case 1:
                            try
                            {
                                if (items[1] != "")
                                {
                                    error = resolve_opcode(items);
                                }
                            }
                            catch
                            {
                            }
                            break;
                    }
                }
                else
                {
                    error = -4;
                } //EOF
                if (error != 0 && lerror != -6)
                {
                    return error;
                }
            }
            return error;
        }

        /// <summary>
        /// Resolve the labels adress
        /// </summary>
        /// <param name="items">the current line</param>
        /// <returns>error index</returns>
        private int resolve_labels(String[] items)
        {
            int adress = B.get_ba() / 16 + bank_index;
            int val = 0;
            if (labels.TryGetValue(items[0], out val))
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
                    if (error != -5)
                    {
                        B.set_sword((ushort)bank_index, ResolveOperand(items[2]));
                        B.write_bank();
                    }
                    break;
                case "2FCADR":
                    lerror = toFCADR(items);
                    return lerror;
            }
            bank_index += 1;
            return 0;
        }

        /// <summary>
        /// Resolve the opcode and add a computed operand
        /// </summary>
        /// <param name="items">the current line</param>
        /// <returns>error index</returns>
        private int resolve_opcode(String[] items)
        {
            ushort opcode = 0;
            ushort adress = 0;
            if (fix.opcode.TryGetValue(items[1], out opcode))
            {
                opcode *= 4096;
            }
            else if (fix.quarter.TryGetValue(items[1], out opcode))
            {
                opcode *= 1024;
            }
            else if (fix.extrac.TryGetValue(items[1], out opcode))
            {
                opcode *= 4096;
            }
            else if (fix.extraq.TryGetValue(items[1], out opcode))
            {
                opcode *= 1024;
            }
            else if (fix.IACode.TryGetValue(items[1], out opcode))
            {
                if (opcode == 7)
                {
                    if (B.isErasable())
                    {
                        B.set_sword((ushort)bank_index, 0x3000);
                    }
                    else if (FB == 2 | FB == 3)
                    {
                        B.set_sword((ushort)bank_index, (ushort)(0x1001 + B.get_ba() + bank_index));
                    }
                    else
                    {
                        B.set_sword((ushort)bank_index, (ushort)(0x1001 + 0x0400 + bank_index));
                    }
                }
                else
                {
                    B.set_sword((ushort)bank_index, (ushort)opcode);
                }
                bank_index++;
                return 0;
            }
            else
            {
                switch (items[1])
                {
                    case "SETLOC":
                        error = SETLOC(items);
                        return 0;
                    case "ERASE":
                        bank_index++;
                        return 0;
                    case "BANK":
                        switch_bank(items);
                        return 0;
                    case "EBANK":
                        switch_bank(items);
                        return 0;
                    case "=":
                        bank_index++;
                        return 0;
                    case "2FCADR":
                        bank_index += 2;
                        return 0;
                    default:
                        bank_index++;
                        return -1;
                }
            }
            adress = ResolveOperand(items[2]);
            B.set_sword((ushort)bank_index, (ushort)(opcode + adress));
            B.write_bank();
            bank_index += 1;
            return 0;
        }

        /// <summary>
        /// Process the SETLOC Pre-processor word
        /// </summary>
        /// <param name="item">The current line</param>
        /// <returns>error index</returns>
        private int SETLOC(string[] item)
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
                {
                    val = keyval;
                }
                else
                {
                    return -6; //unknown label
                }
            }
            if (B.isErasable())
            {
                if (val <= 0xFF)
                {
                    bank_index = val;
                }
                else
                {
                    return -7;
                } //index out of range
            }
            else
            {
                if (val <= 0x3FF)
                {
                    bank_index = val;
                }
                else
                {
                    return -7;
                } //index out of range
            }
            return 0;
        }

        /// <summary>
        /// Process the 2FCADR Pre-processor word
        /// </summary>
        /// <param name="item">the current line</param>
        /// <returns>error index</returns>
        private int toFCADR(string[] item)
        {
            sWord adr = null;
            try
            {
                adr = new sWord((ushort)Int16.Parse(item[2], System.Globalization.NumberStyles.HexNumber), true);
            }
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
                    return -6;
                }
            }
            int tad = adr.getVal(10, 14);
            if (tad < 4)
            {
                B.set_sword((ushort)bank_index, (ushort)(adr.getVal(10, 14)));
            }
            else
            {
                B.set_sword((ushort)bank_index, (ushort)(adr.getVal(10, 14) - 4));
            }
            bank_index++;
            B.set_sword((ushort)bank_index, (ushort)(adr.getVal(0, 9)));
            bank_index++;
            B.write_bank();
            if (FC_count > 0)
            {
                return -6;
            }
            return 0;
        }

        /// <summary>
        /// Compute the operand from the line
        /// </summary>
        /// <param name="item">the current line</param>
        /// <returns>error index</returns>
        private ushort ResolveOperand(string item)
        {
            ushort adress = 0;
            try
            {
                adress = (ushort)Int16.Parse(item, System.Globalization.NumberStyles.HexNumber);
            }
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
                        else
                        {
                            adress = (ushort)val;
                        }
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

        /// <summary>
        /// Switch between banks following the BANK/EBANK pre-processor word
        /// </summary>
        /// <param name="items">the current line</param>
        private void switch_bank(string[] items)
        {
            save_index();
            B.write_bank();
            switch (items[1])
            {
                case "BANK":
                    FB = (ushort)int.Parse(items[2], System.Globalization.NumberStyles.Integer);
                    if (FB > 32)
                    {
                        FEB = 1;
                    }
                    else
                    {
                        FEB = 0;
                    }
                    bank_index = bank_count[FB];
                    bank_type = "FB";
                    break;
                case "EBANK":
                    EB = (ushort)int.Parse(items[2], System.Globalization.NumberStyles.Integer);
                    bank_index = bank_count[EB];
                    bank_type = "EB";
                    break;
            }
            bank_changed = true;

        }

        /// <summary>
        /// Save the bank_index to the bank_index array (bank_count) to keep track of the used area of the bank
        /// </summary>
        private void save_index()
        {
            if (B.isErasable())
            {
                bank_count[B.getId()] = bank_index;
            }
            else
            {
                if (B.getFEB() != 1)
                {
                    bank_count[B.getId() + 8] = bank_index;
                }
                else
                {
                    bank_count[B.getId() + 16] = bank_index;
                }
            }
        }
    }
}
