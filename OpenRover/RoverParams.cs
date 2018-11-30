using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRover
{

    /// <summary>
    /// An enumeration of the parameters which may be queried via the
    /// Open Rover FW serial interface.
    /// TODO: Comment each describing what they mean.
    /// </summary>
    public enum RoverParams
    {
        REG_PWR_TOTAL_CURRENT = 0,  //5hz
        REG_MOTOR_FB_RPM_LEFT = 2, //5hz ------ WIP
        REG_MOTOR_FB_RPM_RIGHT = 4, //5hz ------ WIP
        REG_FLIPPER_FB_POSITION_POT1 = 6, //5hz
        REG_FLIPPER_FB_POSITION_POT2 = 8, //5hz 

        REG_MOTOR_FB_CURRENT_LEFT = 10, //5hz
        REG_MOTOR_FB_CURRENT_RIGHT = 12, //5hz
        REG_MOTOR_FAULT_FLAG_LEFT = 18, //1hz ------ WIP
        REG_MOTOR_TEMP_LEFT = 20, //1hz
        REG_MOTOR_TEMP_RIGHT = 22, //1hz

        REG_POWER_BAT_VOLTAGE_A = 24, //1hz
        REG_POWER_BAT_VOLTAGE_B = 26, //1hz
        ENCODER_INTERVAL_MOTOR_LEFT = 28, //10hz
        ENCODER_INTERVAL_MOTOR_RIGHT = 30, //10hz
        ENCODER_INTERVAL_MOTOR_FLIPPER = 32, //10hz ------ WIP

        REG_ROBOT_REL_SOC_A = 34, //1hz ------ WIP
        REG_ROBOT_REL_SOC_B = 36, //1hz
        REG_MOTOR_CHARGER_STATE = 38,  //5hz
        BUILDNO = 40,  //1hz
        REG_POWER_A_CURRENT = 42,  //5hz

        REG_POWER_B_CURRENT = 44, //5hz
        REG_MOTOR_FLIPPER_ANGLE = 46,  //5hz

        REG_MOTOR_SIDE_FAN_SPEED = 48, //5hz----WIP
        REG_MOTOR_SLOW_SPEED = 50, //5hz ----WIP

        BATTERY_STATUS_A = 52,
        BATTERY_STATUS_B = 54,
        BATTERY_MODE_A = 56,
        BATTERY_MODE_B = 58,
        BATTERY_TEMP_A = 60,
        BATTERY_TEMP_B = 62,

        BATTERY_VOLTAGE_A = 64,
        BATTERY_VOLTAGE_B = 66,
        BATTERY_CURRENT_A = 68,
        BATTERY_CURRENT_B = 70,
    }

}
