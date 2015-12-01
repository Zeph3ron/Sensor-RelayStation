using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpSensorRelayer
{
    /// <summary>
    /// This class represents a notification model and is the same as the table in the database.
    /// </summary>
    public class Notificaton
    {
        public string Location { get; set; }
        public string MovementDetected{ get; set; }
        public string MachineName { get; set; }
    
    }
}
