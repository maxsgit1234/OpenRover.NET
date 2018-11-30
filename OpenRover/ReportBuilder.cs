using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRover
{
    /// <summary>
    /// A class to support building a complete report comprising of multiple
    /// individual parameter value reports, which are received one-at-a-time, 
    /// and in no specified order.
    /// </summary>
    public class ReportBuilder
    {
        /// <summary>
        /// The type of report that is being generated. This determines the 
        /// set of parameters whose values will be included in the final report.
        /// </summary>
        public ReportType Type;

        /// <summary>
        /// A queue of the remaining parameters whose values 
        /// are to be requested.
        /// </summary>
        public Queue<RoverParams> ToRequest;

        /// <summary>
        /// An object whose reference is used for synchronization 
        /// purposes within this class. Enables thread-safety.
        /// </summary>
        private object _Baton = new object();

        /// <summary>
        /// All results so-far-received of various parameters and their 
        /// associated values.
        /// </summary>
        private Dictionary<RoverParams, SerialInPacket> _Results;

        /// <summary>
        /// Construct a ReportBuilder by specifying its type (superfluous) 
        /// and the set of parameters which must be queried before its report
        /// is to-be-considered complete.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="toReq"></param>
        public ReportBuilder(ReportType type, IEnumerable<RoverParams> toReq)
        {
            Type = type;
            ToRequest = new Queue<RoverParams>(toReq);
            _Results = toReq.ToDictionary(i => i, i => (SerialInPacket)null);
        }

        /// <summary>
        /// Shorthand static constructor to creatre a ReportBuilder by 
        /// specifying only the type of report to be generated.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ReportBuilder Create(ReportType type)
        {
            switch (type)
            {
                case ReportType.Fast:
                    return CreateFast();
                case ReportType.Medium:
                    return CreateMedium();
                case ReportType.Slow:
                    return CreateSlow();
                default:
                    throw new Exception("Unrecognized report type: " + type);
            }
        }

        /// <summary>
        /// Create a ReportBuilder to generate a report comprised of those
        /// parameters deemed to be necessary to query with high-frequency.
        /// </summary>
        /// <returns></returns>
        public static ReportBuilder CreateFast()
        {
            return new ReportBuilder(
                ReportType.Fast, RoverReport.FastReportParams);
        }

        /// <summary>
        /// Create a ReportBuilder to generate a report comprised of those
        /// parameters deemed to be necessary to query with either 
        /// high or medium frequency.
        /// </summary>
        /// <returns></returns>
        public static ReportBuilder CreateMedium()
        {
            return new ReportBuilder(ReportType.Medium,
                RoverReport.FastReportParams
                    .Concat(RoverReport.MedReportParams));

        }

        /// Create a ReportBuilder to generate a report comprised of those
        /// parameters deemed to be necessary to query with any frequency:
        /// high, medium, or low.
        public static ReportBuilder CreateSlow()
        {
            return new ReportBuilder(ReportType.Slow,
                RoverReport.FastReportParams
                    .Concat(RoverReport.MedReportParams)
                    .Concat(RoverReport.SlowReportParams));
        }

        /// <summary>
        /// Given the report requested, returns the next parameter whose value
        /// is desired to be queried from the rover. Returns null if all
        /// desired parameters have already been requested.
        /// </summary>
        /// <returns></returns>
        public RoverParams? NextRequest()
        {
            lock (_Baton)
            {
                if (!ToRequest.Any())
                    return null;

                return ToRequest.Dequeue();
            }
        }

        /// <summary>
        /// Call to indicate that a packet has been received from the rover
        /// in response to a request made by the present client. The resultant 
        /// SerialInPacket is added to the set of returned values. If the
        /// resultant SerialInPacket completes the set of all desired parameter
        /// values, this method returns a completed <see cref="RoverReport"/>,
        /// whose values represent the aggregation of all relevant values 
        /// received during the lifetime of this <see cref=" ReportBuilder"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public RoverReport SetResult(SerialInPacket packet)
        {
            lock (_Baton)
            {
                if (!_Results.ContainsKey(packet.Param))
                    return null;

                _Results[packet.Param] = packet;
                if (_Results.All(i => i.Value != null))
                    return new RoverReport(Type, _Results);
                else
                    return null;
            }
        }
    }
}
