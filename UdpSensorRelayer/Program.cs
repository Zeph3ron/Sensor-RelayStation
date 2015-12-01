using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpSensorRelayer
{
    class Program
    {
        static void Main(string[] args)
        {
            RelayingClient udpRelayingClient = new RelayingClient(7000);
            udpRelayingClient.ListenForBroadcast();
        }
    }
}
