using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRover
{
    /// <summary>
    /// A report, assembled from rover responses to individual parameter value
    /// requests. The set of parameters whose values are contained within is
    /// determined by the type of report (somewhat arbitrarily).
    /// </summary>
    public class RoverReport
    {
        /// <summary>
        /// The set of parameters which are to be queried with high frequency.
        /// NOTE: Not all rovers contain encoders (including mine!) so these
        /// parameters may be useless for some users.
        /// </summary>
        public static readonly RoverParams[] FastReportParams = new RoverParams[] {
                RoverParams.ENCODER_INTERVAL_MOTOR_LEFT,
                RoverParams.ENCODER_INTERVAL_MOTOR_RIGHT };

        /// <summary>
        /// The set of parameters which are to be queried with medium frequency.
        /// </summary>
        public static readonly RoverParams[] MedReportParams = new RoverParams[]{
                RoverParams.REG_PWR_TOTAL_CURRENT,
                RoverParams.REG_FLIPPER_FB_POSITION_POT1,
                RoverParams.REG_FLIPPER_FB_POSITION_POT2,
                RoverParams.REG_MOTOR_FB_CURRENT_LEFT,
                RoverParams.REG_MOTOR_FB_CURRENT_RIGHT,
                RoverParams.REG_MOTOR_CHARGER_STATE,
                RoverParams.REG_POWER_A_CURRENT,
                RoverParams.REG_POWER_B_CURRENT,
                RoverParams.REG_MOTOR_FLIPPER_ANGLE,
                RoverParams.BATTERY_CURRENT_A,
                RoverParams.BATTERY_CURRENT_B };

        /// <summary>
        /// The set of parameters which are to be queried with low frequency.
        /// </summary>
        public static readonly RoverParams[] SlowReportParams = new RoverParams[] {
                RoverParams.REG_MOTOR_TEMP_LEFT,
                RoverParams.REG_POWER_BAT_VOLTAGE_A,
                RoverParams.REG_POWER_BAT_VOLTAGE_B,
                RoverParams.REG_ROBOT_REL_SOC_A,
                RoverParams.REG_ROBOT_REL_SOC_B,
                RoverParams.BATTERY_STATUS_A,
                RoverParams.BATTERY_STATUS_B,
                RoverParams.BATTERY_MODE_A,
                RoverParams.BATTERY_MODE_B,
                RoverParams.BATTERY_TEMP_A,
                RoverParams.BATTERY_TEMP_B,
                RoverParams.BATTERY_VOLTAGE_A,
                RoverParams.BATTERY_VOLTAGE_B,
                RoverParams.BUILDNO };


        /// <summary>
        /// The type of this report, which determines the set of parameters 
        /// whose values are contained within.
        /// </summary>
        public ReportType Type;

        /// <summary>
        /// A keyed-collection of packets received from the rover, one for
        /// each of the parameters requested by this report.
        /// </summary>
        public Dictionary<RoverParams, SerialInPacket> Packets;

        /// <summary>
        /// Construct a RoverReport from its type and a keyed-collection of
        /// the packets received which specify the value of each parameter
        /// requested by the report.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="packets"></param>
        public RoverReport(ReportType type,
            Dictionary<RoverParams, SerialInPacket> packets)
        {
            Type = type;
            Packets = packets;
        }

        /// <summary>
        /// Construct an empty report, especially to be aggregated from other
        /// smaller reports later on.
        /// </summary>
        /// <returns></returns>
        public static RoverReport Empty()
        {
            return new RoverReport(ReportType.Aggregate, 
                new Dictionary<RoverParams, SerialInPacket>());
        }

        /// <summary>
        /// Merge the individual parameter values from another report into the
        /// set already maintained by this report. Parameters whose values were
        /// not already maintained will be added; parameters whose values were
        /// already maintained will be updated.
        /// </summary>
        /// <param name="other"></param>
        public void Merge(RoverReport other)
        {
            foreach (var kvp in other.Packets)
                Packets[kvp.Key] = kvp.Value;
        }
        
    }

}
