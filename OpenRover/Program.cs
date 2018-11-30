using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRover
{
    public static class Program
    {
        private enum Motion
        {
            None, Left, Right, Forward, Backward
        }

        private static string _DefaultComPort = "COM5";

        private static bool _PrintReports = false;

        private static RoverChannel _Rover;

        public static void Main(string[] args)
        {
            string comPort = args.Length > 0 ? args[0] : _DefaultComPort;

            _Rover = new RoverChannel(comPort);
            _Rover.NewReport += Rover_NewReport;
            _Rover.Open();
            _Rover.SetLowSpeedMode(true);

            PrintUsage();

            Task.Run(() => SetValues());

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.P:
                        _PrintReports = !_PrintReports; break;
                    case ConsoleKey.I:
                        SetMotion(Motion.Forward); break;
                    case ConsoleKey.J:
                        SetMotion(Motion.Left); break;
                    case ConsoleKey.K:
                        SetMotion(Motion.Backward); break;
                    case ConsoleKey.L:
                        SetMotion(Motion.Right); break;
                    case ConsoleKey.Spacebar:
                        SetMotion(Motion.None); break;
                    default: break;
                }
            }
        }

        private static void SetMotion(Motion m)
        {
            lock (_Baton)
            {
                _LastSet = DateTime.UtcNow;
                _LastMotion = m;
            }
        }

        private static void W(string s)
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Console.WriteLine(now + ": " + s);
        }

        private static void PrintUsage()
        {
            W("USAGE: ");
            W("    OpenRover.exe [comPort]");
            W("        comPort: Optionally specify COM port name. "
                + " Default is " + _DefaultComPort);
            W("");
            W("Use IJKL keys to move the rover.");
            W("Use the space bar to stop the rover.");
            W("Otherwise motion will timeout after " 
                + _Expiration.TotalSeconds + " seconds.");
            W(" ");
            W("Press 'P' to toggle printing of rover reports.");
            W("");
        }

        private static Motion _LastMotion = Motion.None;
        private static DateTime _LastSet = DateTime.MinValue;
        private static TimeSpan _Expiration = TimeSpan.FromSeconds(0.5);
        private static object _Baton = new object();

        public static async Task SetValues()
        {
            while (true)
            {
                await Task.Delay(10);

                Motion m;
                lock (_Baton)
                {
                    DateTime now = DateTime.UtcNow;
                    if (now > _LastSet + _Expiration)
                        _LastMotion = Motion.None;
                    m = _LastMotion;
                }

                IssueCommand(m);
            }
        }

        private static void IssueCommand(Motion m)
        {
            switch (m)
            {
                case Motion.None:
                    _Rover.SetMotorSpeeds(125, 125);
                    break;
                case Motion.Left:
                    _Rover.SetMotorSpeeds(100, 150);
                    break;
                case Motion.Right:
                    _Rover.SetMotorSpeeds(150, 100);
                    break;
                case Motion.Forward:
                    _Rover.SetMotorSpeeds(170, 170);
                    break;
                case Motion.Backward:
                    _Rover.SetMotorSpeeds(80, 80);
                    break;
                default:
                    throw new Exception("Unrecognized motion: " + m);
            }
        }

        private static void Rover_NewReport(RoverReport obj)
        {
            if (!_PrintReports)
                return;

            if (obj.Type != ReportType.Slow)
                return;

            RoverReport rep = _Rover.LastCompleteReport();
            foreach (var kvp in rep.Packets)
            {
                int val = kvp.Value.IntegerValue;
                Console.WriteLine("  " + kvp.Key.ToString().PadLeft(30)
                    + " | " + val.ToString().PadLeft(8));
            }

        }
    }
}
