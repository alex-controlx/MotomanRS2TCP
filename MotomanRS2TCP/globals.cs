using System;
using System.Threading.Tasks;

namespace MotomanRS2TCP
{

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


    public static class TaskEx
    {
        /// <summary>
        /// Blocks while condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the block.</param>
        /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                throw new TimeoutException();
        }

        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                throw new TimeoutException();
        }
    }


    public class IMove
    {
        public IMove(double backForward, double leftRight, double upDown, double acwCw)
        {
            this.backForward = backForward;
            this.leftRight = leftRight;
            this.upDown = upDown;
            this.acwCw = acwCw;
        }

        public double[] ToArray()
        {
            // current button: X:-bk+Frd, Y:-lft+Rght, Z:-up+Down
            // Array is X, Y, Z, Rx, Ry, Rz
            return new double[] { backForward, leftRight, upDown, 0, 0, acwCw, 0, 0, 0, 0, 0, 0 };
        }

        public double backForward { get; set; }
        public double leftRight { get; set; }
        public double upDown { get; set; }
        public double acwCw { get; set; }
    }
}
