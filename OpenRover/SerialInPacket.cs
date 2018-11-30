using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRover
{

    /// <summary>
    /// Represents a packet received from the Open Rover via the FW 
    /// serial interface.
    /// </summary>
    public class SerialInPacket
    {
        /// <summary>
        /// Expected size of the returned packet, in bytes.
        /// </summary>
        public const int PACKET_SIZE = 5;

        /// <summary>
        /// Actual payload received, including start-byte and checksum byte.
        /// </summary>
        public readonly byte[] Bytes;

        /// <summary>
        /// The parameter whose value was returned in this packet.
        /// </summary>
        public readonly RoverParams Param;

        /// <summary>
        /// The integer value of the parameter that was queried.
        /// </summary>
        public readonly int IntegerValue;

        /// <summary>
        /// Creates a SerialInPacket from a 5-byte-long payload, received 
        /// via the FW serial interface.
        /// TODO: Check packet length, first-byte, and check-sum.
        /// </summary>
        /// <param name="bytes"></param>
        public SerialInPacket(byte[] bytes)
        {
            Bytes = bytes;
            Param = (RoverParams)bytes[1];
            IntegerValue = 256 * bytes[2] + bytes[3];
        }


    }
}
