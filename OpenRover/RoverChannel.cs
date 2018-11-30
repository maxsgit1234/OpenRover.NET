using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRover
{
    public interface ISimpleOpenRover
    {
        void SetLowSpeedMode(bool slow);
        void SetMotorSpeeds(byte left, byte rite);
        event Action Closed;
    }

    /// <summary>
    /// Represents a connection to a single Open Rover device via a single
    /// specified serial port.
    /// </summary>
    public class RoverChannel : ISimpleOpenRover
    {
        public static bool DISABLE = false;

        /// <summary>
        /// The only baud rate at which Open Rovers communicate.
        /// </summary>
        private const int BAUD_RATE = 57600;

        /// <summary>
        /// The most-recent value specified for "throttle" of the LEFT motor(s).
        /// </summary>
        public byte LeftMotor { get; private set; }

        /// <summary>
        /// /// The most-recent value specified for "throttle" of the RIGHT motor(s).
        /// </summary>
        public byte RiteMotor { get; private set; }

        /// <summary>
        /// TODO: Please document. I don't have a rover with flippers...
        /// </summary>
        public byte Flipper { get; private set; }

        /// <summary>
        /// True if the specified serial-port connection to the Open Rover has
        /// been opened (implying that commands and requests-for-information 
        /// are being sent of regular sub-second intervals); false otherwise.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// An event fired anytime a packet is received from the Open Rover
        /// device with which this client is connected.
        /// </summary>
        public event Action<SerialInPacket> DataReceived;

        /// <summary>
        /// An event fired anytime a report (constituting multiple individual 
        /// parameter value requests of the rover) is completed.
        /// </summary>
        public event Action<RoverReport> NewReport;

        /// <summary>
        /// An event fired when an exception is thrown while trying to
        /// communicate to the rover via the serial port.
        /// </summary>
        public event Action<SerialErrorReceivedEventArgs> ErrorReceived;

        /// <summary>
        /// An event fired whan the serial port connection to the Open Rover
        /// is closed.
        /// </summary>
        public event Action Closed;

        /// <summary>
        /// The .NET reference to the underlying SerialPort used for 
        /// communication with the rover.
        /// </summary>
        private SerialPort _Port;
        
        /// <summary>
        /// An object whose reference is used as a baton for synchronization. 
        /// Enables thread-safety. Usage is similar to Java's 
        /// "synchronized" keyword.
        /// </summary>
        private readonly object _Baton = new object();

        /// <summary>
        /// A class that is currently generating requests of the Open Rover in
        /// order to complete a report consisting of the values of a set of
        /// its parameters.
        /// </summary>
        private ReportBuilder _Report = null;

        /// <summary>
        /// An aggregate report representing the most-recent value reported
        /// for each of the parameters that could ever be requested of the
        /// Open Rover.
        /// </summary>
        private RoverReport _LastCompleteReport = RoverReport.Empty();

        /// <summary>
        /// Most-recent timestamp that the "fast" report was requested.
        /// </summary>
        private DateTime _LastFastRequest = DateTime.MinValue;

        /// <summary>
        /// Most-recent timestamp that the "medium" report was requested.
        /// </summary>
        private DateTime _LastMedRequest = DateTime.MinValue;

        /// <summary>
        /// Most-recent timestamp that the "slow" report was requested.
        /// </summary>
        private DateTime _LastSlowRequest = DateTime.MinValue;

        /// <summary>
        /// A delay, in milliseconds, to wait before sending any 2
        /// consecutive SerialOutPackets to the connected rover.
        /// </summary>
        private const int LoopDelayMs = 50;

        /// <summary>
        /// Desired maximum frequency of the "fast"-frequency reports. 
        /// NOTE: Not all rovers have encoders, so this may be useless to some 
        /// (including me!)
        /// </summary>
        private const int _FastRequestMs = 50;

        /// <summary>
        /// Desired maximum frequency of the "medium"-frequency reports.
        /// </summary>
        private const int _MedRequestMs = 500;

        /// <summary>
        /// Desired maximum frequency of the "slow"-frequency reports.
        /// </summary>
        private const int _SlowRequestMs = 2000;
        
        /// <summary>
        /// A collection of wait-handles corresponding to external requests for
        /// the value of a specified parameter.
        /// </summary>
        private Dictionary<RoverParams, List<ManualResetEventSlim>> _Waiters
            = new Dictionary<RoverParams, List<ManualResetEventSlim>>();

        /// <summary>
        /// A collection of packets returned in response to requests made on 
        /// threads associated with the specified wait handles. Used to 
        /// temporarily store the values of the retured packets before returning
        /// them to their synchronous callers.
        /// </summary>
        private Dictionary<ManualResetEventSlim, SerialInPacket> _Results
            = new Dictionary<ManualResetEventSlim, SerialInPacket>();
        
        /// <summary>
        /// Construct a new <see cref="RoverChannel"/> that communicates
        /// over the specified serial port.
        /// </summary>
        /// <param name="comPort">The name of the serial port, e.g. "COM3", 
        /// as seen in Windows Device Manager.</param>
        public RoverChannel(string comPort)
        {
            LeftMotor = SerialOutPacket.MOTOR_NEUTRAL;
            RiteMotor = SerialOutPacket.MOTOR_NEUTRAL;

            _Port = new SerialPort(comPort, BAUD_RATE, Parity.None);
            _Port.DataReceived += Port_DataReceived;
            //_Port.ErrorReceived += Port_ErrorReceived;
            
        }

        /// <summary>
        /// Returns true if port was opened successfully; 
        /// false if it was already open.
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            lock (_Baton)
            {
                if (IsOpen)
                    return false;

                if (DISABLE)
                    return true;

                _Port.Open();
                IsOpen = true;

                Task.Run(async () => await RunCommandLoop());

                return true;
            }
        }

        /// <summary>
        /// Returns true if the port was successfully closed; 
        /// false if it was not open.
        /// </summary>
        /// <returns></returns>
        public bool Close()
        {
            lock (_Baton)
            {
                if (!IsOpen)
                    return false;

                if (DISABLE)
                    return true;

                _Port.DataReceived -= Port_DataReceived;
            }

            Thread.Sleep(500);

            lock (_Baton)
            {
                _Port.Close();
                return true;
            }
        }

        /// <summary>
        /// Returns a report containing the most-recently-queried value for
        /// every parameter. <see cref="_LastCompleteReport"/>
        /// </summary>
        /// <returns></returns>
        public RoverReport LastCompleteReport()
        {
            lock (_Baton)
            {
                return _LastCompleteReport;
            }
        }

        /// <summary>
        /// Sets the byte value that shall be commanded to the left motor(s)
        /// hereafter until a new value is specified.
        /// </summary>
        /// <param name="value"></param>
        public void SetLeftMotorSpeed(byte value)
        {
            lock (_Baton)
            {
                LeftMotor = value;
            }
        }

        /// <summary>
        /// Sets the byte value that shall be commanded to the right motor(s)
        /// hereafter until a new value is specified.
        /// </summary>
        /// <param name="value"></param>
        public void SetRiteMotorSpeed(byte value)
        {
            lock (_Baton)
            {
                RiteMotor = value;
            }
        }

        /// <summary>
        /// Simultaneously sets the byte value that shall be commanded to 
        /// both the left and right motor(s) hereafter, until a new value
        /// is specified.
        /// </summary>
        /// <param name="left">A value to command the left motor(s).</param>
        /// <param name="rite">A value to command the right motor(s).</param>
        public void SetMotorSpeeds(byte left, byte rite)
        {
            lock (_Baton)
            {
                LeftMotor = left;
                RiteMotor = rite;
            }
        }

        /// <summary>
        /// Commands the rover to enter a special "low-speed-mode", which
        /// accoring to RoverRobotics documentation, helps prevent the rover
        /// from "running away"....
        /// </summary>
        /// <param name="slow">True if the rover shall be put into low-speed-mode; 
        /// false it shall be returned to "full-speed-mode".</param>
        public void SetLowSpeedMode(bool slow)
        {
            lock (_Baton)
            {
                SendMessage(SerialOutPacket
                    .SlowSpeedMode(LeftMotor, RiteMotor, slow));
            }
        }

        /// <summary>
        /// Requests a response from the rover with the value of a specified
        /// parameter. Synchronoously awaits the result, but does not block the
        /// requesting thread(s).
        /// 
        /// TODO: Make an asynchronous method as well.
        /// </summary>
        /// <param name="param">The parameter whose value is requested.</param>
        /// <returns></returns>
        public SerialInPacket RequestParamSynchronous(RoverParams param)
        {
            
            ManualResetEventSlim waiter = new ManualResetEventSlim();
            lock (_Baton)
            {
                if (!_Waiters.ContainsKey(param))
                    _Waiters.Add(param, new List<ManualResetEventSlim>());
                _Waiters[param].Add(waiter);
                _Results.Add(waiter, null);

                SendMessage(SerialOutPacket
                    .Request(LeftMotor, RiteMotor, param));
            }

            waiter.Wait();
            lock (_Baton)
            {
                SerialInPacket ret = _Results[waiter];
                _Results.Remove(waiter);
                return ret;
            }
        }
        
        /// <summary>
        /// Sends the specified <see cref="SerialOutPacket"/> to the connected
        /// rover.
        /// </summary>
        /// <param name="packet"></param>
        public void SendMessage(SerialOutPacket packet)
        {
            lock (_Baton)
            {
                byte[] bytes = packet.Serialize();

                if (DISABLE)
                    return;

                _Port.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// Runs a loop, so long as an Open Rover is connected on the specified
        /// serial port, which repeatedly commands the motor(s) to the 
        /// most-recently-specified throttles, and issues requests according 
        /// to a report cadence that is specific to both the 
        /// <see cref="ReportType"/> and the configuration of this class.
        /// </summary>
        /// <returns></returns>
        private async Task RunCommandLoop()
        {
            while (IsOpen)
            {
                await Task.Delay(LoopDelayMs);

                RoverParams? param2 = null;
                lock (_Baton)
                {
                    DateTime now = DateTime.UtcNow;
                    if (_Report == null &&
                        (now - _LastFastRequest).TotalMilliseconds > _FastRequestMs)
                    {
                        ReportType type = ReportType.Fast;
                        if ((now - _LastSlowRequest).TotalMilliseconds > _SlowRequestMs)
                        {
                            type = ReportType.Slow;
                            _LastSlowRequest = now;
                            _LastMedRequest = now;
                        }
                        else if ((now - _LastMedRequest).TotalMilliseconds > _MedRequestMs)
                        {
                            type = ReportType.Medium;
                            _LastMedRequest = now;
                        }

                        _LastFastRequest = now;
                        _Report = ReportBuilder.Create(type);
                    }

                    if (_Report != null)
                        param2 = _Report.NextRequest();
                }

                SerialOutPacket packet = param2.HasValue ?
                    SerialOutPacket.Request(LeftMotor, RiteMotor, param2.Value) :
                    SerialOutPacket.MotorCommand(LeftMotor, RiteMotor);

                SendMessage(packet);
            }
        }

        /// <summary>
        /// Updates the state of all wait-handles according to a newly-received
        /// packet from the connected rover.
        /// </summary>
        /// <param name="packet"></param>
        private void UpdateWaiters(SerialInPacket packet)
        {
            if (!_Waiters.ContainsKey(packet.Param))
                return;

            List<ManualResetEventSlim> waiters = _Waiters[packet.Param];
            foreach (ManualResetEventSlim waiter in waiters)
            {
                _Results[waiter] = packet;
                waiter.Set();
            }

            waiters.Clear();
        }

        /// <summary>
        /// Updates the report which is currently being built (if any) with 
        /// the results received from the rover.
        /// </summary>
        /// <param name="packet"></param>
        private void UpdateReport(SerialInPacket packet)
        {
            if (_Report != null)
            {
                RoverReport rep = _Report.SetResult(packet);
                if (rep != null)
                {
                    _LastCompleteReport.Merge(rep);
                    NewReport?.Invoke(rep);
                    _Report = null;
                }
            }
        }

        /// <summary>
        /// An event-handler, fired anytime data is received via the 
        /// connected serial port.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Port_DataReceived(
            object sender, SerialDataReceivedEventArgs e)
        {
            if (!IsOpen)
                return;

            // breaks-up the response into 5-byte packets, as specified in 
            // the OpenRover FW API...
            while (_Port.BytesToRead >= SerialInPacket.PACKET_SIZE)
            {
                lock (_Baton)
                {
                    if (!IsOpen)
                        return;

                    byte[] b = new byte[SerialInPacket.PACKET_SIZE];
                    int nb2 = _Port.Read(b, 0, b.Length);

                    SerialInPacket packet = new SerialInPacket(b);

                    DataReceived?.Invoke(packet);

                    lock (_Baton)
                    {
                        UpdateReport(packet);
                        UpdateWaiters(packet);
                    }
                }
            }
        }

        /// <summary>
        /// An event-handler, fired anytime an error is received via the
        /// connected serial port.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Port_ErrorReceived(
            object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorReceived?.Invoke(e);
        }

    }
}
