using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AGC_SUPPORT;
using System.Threading;

namespace nDSKY
{
    public class tDSKY
    {
        Channels chan;

        public tDSKY(Channels chan)
        {
            this.chan = chan;
            chan.regDelegate(this, read_chan);
        }

        public void DStart()
        {
            int i = 0;
            while(i < 25)
            {
                Thread.Sleep(10);
                write_chan(0, 2);
                i++;
            }
        }

        public void write_chan(int index, ushort value)
        {
            chan.set_chan(this, index, value);
        }

        public void read_chan(int index)
        {
            Console.WriteLine("AGC Wrote Index {0} - Value {1}", index, chan.get_chan(index));
        }
    }
}
