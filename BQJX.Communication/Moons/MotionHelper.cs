using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.Moons
{
    internal static class MotionHelper
    {
        public static string Homing(ushort addr, int homeVel)
        {
            string command = addr.ToString("X1") + "VE" + homeVel.ToString() + "\r\n";
            command += addr.ToString("X1") + "DI-1000" + "\r\n";
            command += addr.ToString("X1") + "SH1L" + "\r\n";
            command += addr.ToString("X1") + "EP0" + "\r\n";
            return command;
        }
        public static string HardStopHoming(ushort addr, int current)
        {
            string command = addr.ToString("X1") + "HA0.2" + "\r\n";                   //加速度设置
            command += addr.ToString("X1") + "HC" + current.ToString() + "\r\n";       //停止电流
            command += addr.ToString("X1") + "HO-10" + "\r\n";                       //回零偏移
            command += addr.ToString("X1") + "HL1" + "\r\n";                         //减速度设置
            command += addr.ToString("X1") + "HV1 0.5" + "\r\n";                     //回零速度1
            command += addr.ToString("X1") + "HV2 0.1" + "\r\n";                     //回零速度2
            command += addr.ToString("X1") + "HV3 0.1" + "\r\n";                     //回零速度3
            command += addr.ToString("X1") + "HS1" + "\r\n";                         //触发硬限位回零
            command += addr.ToString("X1") + "EP0" + "\r\n";                         //清零
            return command;
        }
        public static string Call_Q_Program(ushort addr, int segment)
        {
            string command = addr.ToString("X1") + "QX" + segment.ToString() + "\r\n";
            return command;
        }
        public static string Read_IO_Status(ushort addr)
        {
            string command = addr.ToString("X1") + "IO" + "\r\n";
            return command;
        }
        public static string JogF(ushort addr, int vel)
        {
            //DI-1   //JS1  // JA100  //JL100  //CJ
            string cmd = "JS,JA100, JL100,DI1000,CJ";
            string command = null;
            string[] cmds = cmd.Split(',');
            cmds[0] = addr.ToString("X1") + cmds[0] + vel.ToString() + "\r\n";
            cmds[1] = addr.ToString("X1") + cmds[1] + "\r\n";
            cmds[2] = addr.ToString("X1") + cmds[2] + "\r\n";
            cmds[3] = addr.ToString("X1") + cmds[3] + "\r\n";
            cmds[4] = addr.ToString("X1") + cmds[4] + "\r\n";
            foreach (string str in cmds)
            {
                command += str;
            }
            return command;
        }
        public static string JogR(ushort addr, int vel)
        {
            string cmd = "JS,JA100, JL100,DI-1000,CJ";
            string command = null;
            string[] cmds = cmd.Split(',');
            cmds[0] = addr.ToString("X1") + cmds[0] + vel.ToString() + "\r\n";
            cmds[1] = addr.ToString("X1") + cmds[1] + "\r\n";
            cmds[2] = addr.ToString("X1") + cmds[2] + "\r\n";
            cmds[3] = addr.ToString("X1") + cmds[3] + "\r\n";
            cmds[4] = addr.ToString("X1") + cmds[4] + "\r\n";
            foreach (string str in cmds)
            {
                command += str;
            }
            return command;
        }
        public static string StopJogging(ushort addr)
        {
            //CJ
            string command = addr.ToString("X1") + "SJ" + "\r\n";
            return command;
        }
        public static string VelSetting(ushort addr, int vel)
        {
            string command = addr.ToString("X1") + "VE" + vel.ToString() + "\r\n";
            return command;
        }
        /// <summary>
        /// SK,SM,ST=STOP&KILL ;STOPMOVE;STOP
        /// </summary>
        /// <param name="add"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MoveDistance(ushort addr, int vel, int distance)
        {
            string command = addr.ToString("X1") + "VE" + vel.ToString() + "\r\n";
            command += addr.ToString("X1") + "FP" + distance.ToString() + "\r\n";
            return command;
        }
        public static string MoveRelative(ushort addr, int vel, int distance)
        {
            string command = addr.ToString("X1") + "VE" + vel.ToString() + "\r\n";
            command += addr.ToString("X1") + "FL" + distance.ToString() + "\r\n";
            return command;
        }
        public static string StopMotion(ushort addr)
        {
            string command = addr.ToString("X1") + "STD" + "\r\n";
            return command;
        }
        public static string ReadPositon(ushort addr)
        {
            string command = addr.ToString("X1") + "IP" + "\r\n";
            return command;
        }
        public static string ResetAlm(ushort addr)
        {
            string command = addr.ToString("X1") + "AR" + "\r\n";
            return command;
        }
        /// <summary>
        /// Hex Value SSM/TSM/TXM SS M2
        ///0001 Position Limit
        ///0002 CCW Limit
        ///0004 CW Limit
        ///0008 Over Temp
        ///0010 Internal Voltage
        ///0020 Over Voltage
        ///0040 Under Voltage Under Voltage Under Voltage
        ///0080 Over Current
        ///0100 Open Motor Winding Open Motor Winding Bad Hall
        ///0200 Bad Encoder
        ///0400 Comm Error
        ///0800 Bad Flash
        ///1000 No Move No Move Excess Regen
        ///2000 Current Foldback
        ///4000 Bland Q Segment
        ///8000 NV Memory Double Error NV Memory Double Error No Move
        /// </summary>
        /// <param name="add"></param>
        public static string ReadAlm(ushort addr)
        {
            //1AL=0000
            string command = addr.ToString("X1") + "AL" + "\r\n";
            return command;
        }
        /// <summary>
        ///0001 Motor Enabled (Motor Disabled if this bit = 0)
        ///0002 Sampling(for Quick Tuner)
        ///0004 Drive Fault(check Alarm Code)
        ///0008 In Position(motor is in position)
        ///0010 Moving(motor is moving)
        ///0020 Jogging(currently in jog mode)
        ///0040 Stopping(in the process of stopping from a stop command)
        ///0080 Waiting(for an input; executing a WI command)
        ///0100 Saving(parameter data is being saved)
        ///0200 Alarm present(check Alarm Code)
        ///0400 Homing(executing an SH command)
        ///0800 Waiting(for time; executing a WD or WT command)
        ///1000 Wizard running(Timing Wizard is running)
        ///2000 Checking encoder(Timing Wizard is running)
        ///4000 Q Program is running
        ///8000 Initializing(happens at power up)
        /// </summary>
        /// <param name="add"></param>
        public static string ReadStatus(ushort addr)
        {
            //1SC = 0009
            string command = addr.ToString("X1") + "SC" + "\r\n";
            return command;
        }
        public static string Enable(ushort addr)
        {
            string command = addr.ToString("X1") + "ME" + "\r\n";
            return command;
        }
        public static string Disable(ushort addr)
        {
            string command = addr.ToString("X1") + "MD" + "\r\n";
            return command;
        }
        public static string SetZero(ushort addr)
        {
            string command = addr.ToString("X1") + "EP0" + "\r\n";
            return command;
        }
        /// <summary>
        /// 解析返回数据
        /// </summary>
        /// <param name="mes"></param>
        /// <returns></returns>
        public static object AnalysisResponseData(string mes)
        {
            if (string.IsNullOrWhiteSpace(mes) || string.IsNullOrEmpty(mes))//返回数据为空
            {
                throw new Exception("数据接收超时！ 返回字符为空");
            }
            if (mes.Substring(1, 1) == "%") //写入数据返回
            {
                return true;
            }
            if (mes.Substring(1, 1) == "?") //写入数据返回
            {
                switch (mes.Substring(2, 1))
                {
                    case "1":
                        if (mes.Length == 3)
                        {
                            throw new Exception("Command timed out");
                        }
                        switch (mes.Substring(3, 1))
                        {
                            case "0":
                                throw new Exception("Comm port error");
                            case "1":
                                throw new Exception("Bad character");
                            case "2":
                                throw new Exception("I/O point already used by current Command Mode, and cannot be changed");
                            case "3":
                                throw new Exception("I/O point configured for incorrect use");
                            case "4":
                                throw new Exception("I/O point cannot be used for requested function - see HW manual for possible I/O function assignments");
                            default:
                                throw new Exception("未知错误！");
                        }
                    case "2":
                        throw new Exception("Parameter is too long");
                    case "3":
                        throw new Exception("Too few parameters");
                    case "4":
                        throw new Exception("Too many parameters");
                    case "5":
                        throw new Exception("Parameter out of range");
                    case "6":
                        throw new Exception("Command buffer (queue) full");
                    case "7":
                        throw new Exception("Cannot process command");
                    case "8":
                        throw new Exception("Program running");
                    case "9":
                        throw new Exception("Bad password");
                    default:
                        throw new Exception("未知错误！");
                }
            }
            if (mes.Substring(1, 1) == "*") //写入数据返回
            {
                throw new Exception("从机忙！");
            }
            if (mes.Substring(1, 2) == "IP") //解析位置数据
            {
                string value = mes.Substring(mes.IndexOf("=") + 1, mes.Length - 5);
                return int.Parse(value);
            }
            if (mes.Substring(1, 2) == "AL")//解析报警数据
            {
                int value = int.Parse(mes.Substring(mes.IndexOf("=") + 1, mes.Length - 5));
                return value;
            }
            if (mes.Substring(1, 2) == "AR")//解析报警数据
            {
                int value = int.Parse(mes.Substring(mes.IndexOf("=") + 1, mes.Length - 5));
                return value;
            }
            if (mes.Substring(1, 2) == "SC")//解析状态数据
            {
                int value = int.Parse(mes.Substring(mes.IndexOf("=") + 1, mes.Length - 5), System.Globalization.NumberStyles.HexNumber);
                return value;
            }
            throw new Exception($"数据校验出错：{mes}");
        }
    }
}
