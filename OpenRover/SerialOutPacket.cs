using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRover
{
    /// <summary>
    /// Represents a packet to-be-delivered to the Open Rover via the FW
    /// serial interface. Represents a motor (or flipper) command and 
    /// [possibly also] a request for information about a single FW parameter.
    /// </summary>
    public class SerialOutPacket
    {
        /// <summary>
        /// The value corresponding to no motion of the motors/flippers. 
        /// Greater values move the rover forward; 
        /// lesser values move it in reverse.
        /// </summary>
        public const byte MOTOR_NEUTRAL = 125;

        /// <summary>
        /// A reserved byte value indicating the start of a packet.
        /// </summary>
        public const byte START_BYTE = 253;

        /// <summary>
        /// A value which, if set in the 5th byte of the packet, is
        /// interpreted by the rover as a request for the value of the 
        /// parameter specified in the 6th byte.
        /// </summary>
        public const byte REQUEST_FLAG = 10;

        /// <summary>
        /// A value which, if set in the 5th byte of the packet, is
        /// interpreted by the rover as a request to toggle "low speed mode",
        /// in a polarity determined by the 6th byte (1 = slow; 0 = fast/normal).
        /// </summary>
        public const byte SLOW_SPEED_FLAG = 240;

        /// <summary>
        /// A value commanded to the left motor(s), if applicable.
        /// </summary>
        public byte Left;

        /// <summary>
        /// A value commanded to the right motor(s), if applicable.
        /// </summary>
        public byte Rite;

        /// <summary>
        /// TODO: Document this.
        /// </summary>
        public byte Flipper;

        /// <summary>
        /// A flag which can affect rover configuration and state when set to
        /// one of a set of special values.
        /// </summary>
        public byte Param1;

        /// <summary>
        /// A secondary value, whose meaning depends of the value set for 
        /// <see cref="Param1"/>
        /// </summary>
        public byte Param2;

        /// <summary>
        /// Construct a SerialOutPacket by specifying each of the data bytes
        /// explicitly.
        /// </summary>
        /// <param name="left">A value to command the left motor(s).</param>
        /// <param name="rite">A value to command the right motor(s).</param>
        /// <param name="flipper">TODO</param>
        /// <param name="param1"><see cref="Param1"/></param>
        /// <param name="param2"><see cref="Param2"/></param>
        public SerialOutPacket(byte left, byte rite, byte flipper,
            byte param1, byte param2)
        {
            Left = left;
            Rite = rite;
            Flipper = flipper;
            Param1 = param1;
            Param2 = param2;
        }

        /// <summary>
        /// Construct a SerialOutPacket that commands the rover to toggle
        /// in or out of the special "low speed mode."
        /// </summary>
        /// <param name="left">A value to command the left motor(s).</param>
        /// <param name="rite">A value to command the right motor(s).</param>
        /// <param name="slow">True if the rover should be set to 
        /// low-speed-mode; false otherwise.</param>
        /// <returns></returns>
        public static SerialOutPacket SlowSpeedMode(
            byte left, byte rite, bool slow)
        {
            return new SerialOutPacket(left, rite,
                MOTOR_NEUTRAL, SLOW_SPEED_FLAG, (byte)(slow ? 1 : 0));
        }

        /// <summary>
        /// Construct a SerialOutPacket that makes no request for information
        /// from the rover; it only issues commands for the left and right
        /// motors.
        /// </summary>
        /// <param name="left">A value to command the left motor(s).</param>
        /// <param name="rite">A value to command the right motor(s).</param>
        /// <returns></returns>
        public static SerialOutPacket MotorCommand(byte left, byte rite)
        {
            return new SerialOutPacket(left, rite, MOTOR_NEUTRAL, 0, 0);
        }

        /// <summary>
        /// Construct a SerialOutPacket that requests a response from the rover
        /// for the value of a specified parameter.
        /// </summary>
        /// <param name="left">A value to command the left motor(s).</param>
        /// <param name="rite">A value to command the right motor(s).</param>
        /// <param name="param">The parameter whose values is requested.</param>
        /// <returns></returns>
        public static SerialOutPacket Request(
            byte left, byte rite, RoverParams param)
        {
            return new SerialOutPacket(left, rite, MOTOR_NEUTRAL,
                REQUEST_FLAG, (byte)(int)param);
        }

        /// <summary>
        /// Returns a 7-byte packet that can be sent to the rover to issue 
        /// the specified command and/or request for information.
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            byte[] msg = new byte[7];
            msg[0] = START_BYTE;
            msg[1] = Left;
            msg[2] = Rite;
            msg[3] = Flipper;
            msg[4] = Param1;
            msg[5] = Param2;
            msg[6] = (byte)(255 - (msg[1] + msg[2] + msg[3] + msg[4] + msg[5]) % 255);

            return msg;

        }

    }
}
