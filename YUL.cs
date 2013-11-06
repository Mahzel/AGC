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
        BANK Bank;
        const int max_pass = 5;
        int bank_index = 0;
        int[] bank_index_counter;
        Dictionary<String, int> labels = new Dictionary<String, int>();
        fvDict fixedValue = new fvDict();
        bool bank_changed;
        int error = 0;
        int pass_count = 0;
        int unresolvedLabelError = -6;
        int unresolvedFCAdrCount = 0;
        string bank_type = "FB";
        string filename;
        int line_num = 0;
        Dictionary<int, string> YULErrors;

        /// <summary>
        /// Constructor for the compiler
        /// </summary>
        /// <param name="FInput">The AGC Code file to read from</param>
        /// <param name="FOutput">The AGC Bin file to write to</param>
        public YUL(String FInput, String FOutput)
        {
            AGC_Code_File = FInput;
            filename = Path.GetFileName(AGC_Code_File);
            AGC_Bit_File = FOutput;
            if (File.Exists(AGC_Bit_File))
            {
                File.Delete(AGC_Bit_File);
            }
            AGC_Bit = File.Create(AGC_Bit_File);
            for (int j = 0; j < 655360; j++)
            {
                AGC_Bit.Write(new byte[] { 0 }, 0, 1);
            }
            AGC_Bit.Close();
            AGC_Bit.Dispose();
            Console.WriteLine("Empty bin file created");
            bank_index_counter = new int[44];
            for (int i = 0; i < 44; i++)
            {
                bank_index_counter[i] = 0;
            }
            Cp_File = File.ReadAllLines(AGC_Code_File);
            FB = 0;
            EB = 0;
            FEB = 0;
            unresolvedLabelError = -6;
            Bank = new BANK(false, FB, FEB, AGC_Bit_File);
            Bank.compiling = true;
            bank_changed = false;
        }
        //Run function
        /// <summary>
        /// The compilation routine process lines in mode 0 (resolve labels) then in mode 1 (resolve opcodes) and create the summary output file.
        /// A maximum of 5 passes is granted to process labels.
        /// </summary>
        /// <returns>Error index</returns>
        public int compile()
        {
            while (unresolvedLabelError == -6 && pass_count < max_pass)
            { //until all labels are resolved
                unresolvedFCAdrCount = 0;
                FB = 0;
                EB = 0;
                FEB = 0;
                bank_changed = true;
                for (int i = 0; i < 44; i++)
                {
                    bank_index_counter[i] = 0;
                }
                bank_index = 0;
                error = process_line(0);
                if (error != 0)
                {
                    string errString = errorProcessing();
                    Console.WriteLine("Error at Line {0} return {1} : {2} ", line_num +1, error, errString);
                    return error;
                }
                pass_count++;
                Console.WriteLine("Labels pass : {0}", pass_count);
                if (labels.Keys.Count() == 0)
                {
                    Console.WriteLine("No labels found");
                    unresolvedLabelError = 0;
                }
            }
            if (pass_count == max_pass)
            {
                return -6;
            }
            Console.WriteLine("Done");
            FB = 0;
            EB = 0;
            FEB = 0;
            bank_changed = true;
            for (int i = 0; i < 43; i++)
            {
                bank_index_counter[i] = 0;
            }
            bank_index = 0;
            Console.WriteLine("Compiling...");
            error = process_line(1);
            if (error != 0)
            {
                string errString = errorProcessing();
                Console.WriteLine("Error at Line {0} return {1} : {2} ", line_num+1, error, errString);
                return error;
            }
            Bank.write_bank();
            save_index();
            if (labels.Keys.Count != 0)
            {
                Console.WriteLine("Writing labels recap file");
                output_labels();
                Console.WriteLine("Done.");
            }
            return 0;
        }

        //processing unit
        /// <summary>
        /// Read the AGC Code file and process lines
        /// </summary>
        /// <param name="mode">0 : process labels - 1: process opcodes</param>
        /// <returns>error index</returns>
        private int process_line(int mode)
        {
            String current;
            char[] sep = new char[] { '\t' };
            for (line_num = 0; line_num < Cp_File.Length; line_num++)
            {
                if ((current = Cp_File[line_num]) != null)
                {
                    if (bank_changed)
                    {
                        switchBank();
                    }
                    String[] items = current.Split(sep, StringSplitOptions.None);
                    if (!items[0].Contains("#"))
                    {
                        switch (mode)
                        {
                            case 0:
                                labelMode(items);
                                break;
                            case 1:
                                compileMode(items);
                                break;
                        }
                    }
                }
                else
                {
                    error = -4;
                } //EOF
                if (!check_index())
                {
                    error = -7;
                }
                if (error != 0)
                { return error; }
            }
            if (unresolvedFCAdrCount == 0)
            {
                unresolvedLabelError = 0; ;
            }
            return error;
        }
        private void compileMode(String[] items)
        {
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
        }
        private void labelMode(String[] items)
        {
            if (items[0] != "")
            {
                error = resolve_labels(items);
            }
            else
            {
                try
                {
                    error = preProcessorOp(items, false);
                }
                catch
                {
                }
            }
        }
        private string errorProcessing()
        {
            fillErrorDict();
            string val;
            YULErrors.TryGetValue(error, out val);
            return val;
        }

        //resolving unit
        /// <summary>
        /// Resolve the labels adress
        /// </summary>
        /// <param name="items">the current line</param>
        /// <returns>error index</returns>
        private int resolve_labels(String[] items)
        {
            int adress = Bank.get_ba() / 16 + bank_index;
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
                    return assignValue(items, false);
                case "2FCADR":
                    unresolvedLabelError = toFCADR(items, false);
                    return 0;
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
            if (fixedValue.opcode.TryGetValue(items[1], out opcode) || fixedValue.extrac.TryGetValue(items[1], out opcode))
            {
                opcode *= 4096;
            }
            else if (fixedValue.quarter.TryGetValue(items[1], out opcode) || fixedValue.extraq.TryGetValue(items[1], out opcode))
            {
                opcode *= 1024;
            }
            else if (fixedValue.IACode.TryGetValue(items[1], out opcode))
            {
                if (opcode == 7)
                {
                    if (Bank.isErasable())
                    {
                        Bank.set_word((ushort)bank_index, 0x3000);
                    }
                    else if (FB == 2 | FB == 3)
                    {
                        Bank.set_word((ushort)bank_index, (ushort)(0x1001 + Bank.get_ba() + bank_index));
                    }
                    else
                    {
                        Bank.set_word((ushort)bank_index, (ushort)(0x1001 + 0x0400 + bank_index));
                    }
                }
                else
                {
                    Bank.set_word((ushort)bank_index, (ushort)opcode);
                }
                bank_index++;
                return 0;
            }
            else
            {
                return preProcessorOp(items, true);
            }
            error = ResolveOperand(items[2]);
            if (error == -1)
            { return error; }
            else { adress = (ushort)error; }
            Bank.set_word((ushort)bank_index, (ushort)(opcode + adress));
            bank_index += 1;
            return 0;
        }
        /// <summary>
        /// Compute the operand from the line
        /// </summary>
        /// <param name="item">the current line</param>
        /// <returns>error index</returns>
        private int ResolveOperand(string item)
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
                else if (fixedValue.registers.TryGetValue(item, out val))
                {
                    adress = (ushort)val;
                }
                else
                {
                    return -2;
                }
            }
            return adress;
        }

        //pre-processor resolver
        private int preProcessorOp(String[] items, bool mode)
        {
            switch (items[1])
            {
                case "SETLOC":
                    return SETLOC(items);
                case "ERASE":
                    if (mode) { Bank.set_word((ushort)bank_index, 0); }
                    bank_index++;
                    return 0;
                case "BANK":
                    return prepareBankSwitch(items);
                case "EBANK":
                    return prepareBankSwitch(items);
                case "=":
                    return assignValue(items, mode);
                case "2FCADR":
                    unresolvedFCAdrCount = toFCADR(items, mode);
                    return 0;
                default:
                    ushort val;
                    if (fixedValue.opcode.TryGetValue(items[1], out val) ||
                        fixedValue.quarter.TryGetValue(items[1], out val) ||
                        fixedValue.extraq.TryGetValue(items[1], out val) ||
                        fixedValue.IACode.TryGetValue(items[1], out val) ||
                        fixedValue.IACode.TryGetValue(items[1], out val) ||
                        fixedValue.extrac.TryGetValue(items[1], out val))
                    { return 0; }
                    return -1;
            }
        }
        private int assignValue(String[] items, bool mode)
        {
            error = ResolveOperand(items[2]);
            if (error == -1)
            { return error; }
            if (mode) { Bank.set_word((ushort)bank_index, (ushort)error); }
            bank_index++;
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
            if (Bank.isErasable())
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
        private int toFCADR(string[] item, bool mode)
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
                    unresolvedFCAdrCount += 1;
                    return -6;
                }
            }
            if (mode)
            {
                int tad = adr.getVal(10, 14);
                if (tad < 4)
                {
                    Bank.set_word((ushort)bank_index, (ushort)(adr.getVal(10, 14)));
                }
                else
                {
                    Bank.set_word((ushort)bank_index, (ushort)(adr.getVal(10, 14) - 4));
                }
                bank_index++;
                Bank.set_word((ushort)bank_index, (ushort)(adr.getVal(0, 9)));
                bank_index++;
            }
            if (unresolvedFCAdrCount > 0)
            {
                return -6;
            }
            return 0;
        }

        //Bank management
        /// <summary>
        /// Switch between banks following the BANK/EBANK pre-processor word
        /// </summary>
        /// <param name="items">the current line</param>
        private int prepareBankSwitch(string[] items)
        {
            save_index();
            Bank.write_bank();
            switch (items[1])
            {
                case "BANK":
                    FB = (ushort)int.Parse(items[2], System.Globalization.NumberStyles.Integer);
                    if (FB <= 35 && FB >= 0)
                    {
                        if (FB > 32)
                        { FEB = 1; }
                        else
                        { FEB = 0; }
                        bank_index = bank_index_counter[FB];
                        bank_type = "FB";
                        break;
                    }
                    else { return -3; }
                case "EBANK":
                    EB = (ushort)int.Parse(items[2], System.Globalization.NumberStyles.Integer);
                    if (EB >= 0 && EB <= 7)
                    {
                        bank_index = bank_index_counter[EB];
                        bank_type = "EB";
                        break;
                    }
                    else { return -3; }
            }
            bank_changed = true;
            return 0;
        }
        private void switchBank()
        {
            switch (bank_type)
            {
                case "FB":
                    Bank = new BANK(false, FB, FEB, AGC_Bit_File);
                    break;
                case "EB":
                    Bank = new BANK(true, EB, 0, AGC_Bit_File);
                    break;
            }
            Bank.compiling = true;
            bank_changed = false;
        }
        /// <summary>
        /// Save the bank_index to the bank_index array (bank_index_counter) to keep track of the used area of the bank
        /// </summary>
        private void save_index()
        {
            if (Bank.isErasable())
            {
                bank_index_counter[Bank.getId()] = bank_index;
            }
            else
            {
                if (Bank.getFEB() != 1)
                {
                    bank_index_counter[Bank.getId() + 8] = bank_index;
                }
                else
                {
                    bank_index_counter[Bank.getId() + 16] = bank_index;
                }
            }
        }
        private bool check_index()
        {
            switch (bank_type)
            {
                case "FB":
                    if (bank_index > 1023)
                    { return false; }
                    return true;
                case "EB":
                    if (bank_index > 255)
                    { return false; }
                    return true;
                default:
                    return true;
            }
        }

        //Miscallaneous functions
        /// <summary>
        /// Print labels list and memory status to "Labels_"+AGC_Code_File
        /// </summary>
        private void output_labels()
        {
            if (File.Exists("Labels_" + filename))
            {
                File.Delete("Labels_" + filename);
            }
            FileStream fs = File.Create("Labels_" + filename);
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
                output = String.Format("EBank : {0} - {1} word(s) used. \n", i, bank_index_counter[i]);
                sw.Write(output);
            }
            sw.Write("=============================\n");
            for (int i = 0; i <= 31; i++)
            {
                output = String.Format("FBank : {0} - {1} word(s) used. \n", i, bank_index_counter[i + 8]);
                sw.Write(output);
            }
            sw.Write("=============================\n");
            for (int i = 32; i <= 35; i++)
            {
                output = String.Format("SuperBank : {0} - {1} word(s) used. \n", i, bank_index_counter[i + 8]);
                sw.Write(output);
            }
            sw.Close();
        }
        private void fillErrorDict()
        {
            YULErrors = new Dictionary<int, string>();
            YULErrors.Add(0, "no error");
            YULErrors.Add(-1, "Unknown Opcode");
            YULErrors.Add(-2, "Invalid Operand");
            YULErrors.Add(-3, "Invalid Bank ID");
            YULErrors.Add(-4, "End of File reached");
            YULErrors.Add(-5, "Label already exist");
            YULErrors.Add(-6, "Unresolved label");
            YULErrors.Add(-7, "Bank index out of range");
        }

    }

}
