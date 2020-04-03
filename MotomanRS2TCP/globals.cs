using System;
using System.Collections.Generic;
using System.Text;

namespace MotomanRS2TCP
{
    internal class Timeout : System.Timers.Timer
    {
        public Timeout(Action action, double delay)
        {
            this.AutoReset = false;
            this.Interval = delay;
            this.Elapsed += (sender, args) => action();
            this.Start();
        }
    }

    public enum FrameType : byte
    {
        Base = 0,
        Robot,
        User1, User2, User3, User4, User5, User6, User7, User8,
        User9, User10, User11, User12, User13, User14, User15, User16,
        User17, User18, User19, User20, User21, User22, User23, User24,
        User25, User26, User27, User28, User29, User30, User31, User32,
        User33, User34, User35, User36, User37, User38, User39, User40,
        User41, User42, User43, User44, User45, User46, User47, User48,
        User49, User50, User51, User52, User53, User54, User55, User56,
        User57, User58, User59, User60, User61, User62, User63, User64,
        Tool,
        MasterTool
    }

    public enum VarType : byte
    {
        Byte=0,
        Integer,
        Double,
        Real
    }

    public enum PosVarType : byte
    {
        Pulse=0,
        XYZ
    }

    public class EnumComms
    {
        public const string
            Connecting = "Connecting",
            Connected = "Connected",
            Error = "Error",
            Disconnecting = "Disconnecting",
            Disconnected = "Disconnected";
    }

    public class EnumEvent
    {
        public const string
            Idle = "Idle",
            GettingStatus = "Getting Status",
            Setting  = "Disconnected";
    } 
}
