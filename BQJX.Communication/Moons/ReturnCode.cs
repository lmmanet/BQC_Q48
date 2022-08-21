using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.Moons
{
    public enum StatusCode
    {
        MotorEnable = 0x0001,         //0001 Motor Enabled (Motor Disabled if this bit = 0)
        Sampling = 0x0002,            //0002 Sampling(for Quick Tuner)
        DriveFault = 0x0004,          //0004 Drive Fault(check Alarm Code)
        InPosition = 0x0008,          //0008 In Position(motor is in position)
        Moving = 0x0010,              //0010 Moving(motor is moving)
        Jogging = 0x0020,             //0020 Jogging(currently in jog mode)
        Stopping = 0x0040,            //0040 Stopping(in the process of stopping from a stop command)
        WaitingForInput = 0x0080,     //0080 Waiting(for an input; executing a WI command)
        Saving = 0x0100,              //0100 Saving(parameter data is being saved)
        Alarm = 0x0200,               //0200 Alarm present(check Alarm Code)
        Homing = 0x0400,              //0400 Homing(executing an SH command)
        WaitingForTime = 0x0800,      //0800 Waiting(for time; executing a WD or WT command)
        WizardRunning = 0x1000,       //1000 Wizard running(Timing Wizard is running)
        CheckingEncoder = 0x2000,     //2000 Checking encoder(Timing Wizard is running)
        Q_Program_is_running = 0x4000,//4000 Q Program is running
        Initializing = 0x8000         //8000 Initializing(happens at power up)           
    }

    public enum AlmCode
    {
        PositionLimit = 0x0001,                //0001 Position Limit
        CCW_Limit = 0x0002,                    //0002 CCW Limit
        CW_Limit = 0x0004,                     //0004 CW Limit
        Over_Temp = 0x0008,                    //0008 Over Temp
        Internal_Voltage = 0x0010,             //0010 Internal Voltage
        Over_Voltage = 0x0020,                 //0020 Over Voltage
        Under_Voltage = 0x0040,                //0040 Under Voltage Under Voltage Under Voltage
        Over_Current = 0x0080,                 //0080 Over Current
        Open_Motor_Winding = 0x0100,           //0100  Open Motor Winding Bad Hall
        Bad_Encoder = 0x0200,                  //0200 Bad Encoder
        Cmd_Error = 0x0400,                    //0400 Comm Error
        Bad_Flash = 0x0800,                    //0800 Bad Flash
        No_Move = 0x1000,                      //1000 No Move No Move Excess Regen
        Current_Foldback = 0x2000,             //2000 Current Foldback
        Bland_Q_Segment = 0x4000,              //4000 Bland Q Segment
        NV_Memory_Double_Error = 0x8000        //8000  NV Memory Double Error No Move
    }
}
