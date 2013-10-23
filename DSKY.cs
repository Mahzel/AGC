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
            while(true)
            {
                Thread.Sleep(100);
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
