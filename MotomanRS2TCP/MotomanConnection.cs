using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MotomanRS2TCP
{

    public class MotomanConnection
    {
        #region member variables

        private short m_Handle = -1;
        System.Windows.Forms.Timer StatusTimeout = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer CommsChecker = new System.Windows.Forms.Timer();
        //private static Object m_FileAccessDirLock = new Object();
        //private Object m_YasnacAccessLock = new Object();
        private static string m_CommDir;
        private string m_CurrentConnection = EnumComms.Disconnected;
        private string m_Error = "";
        private string m_CurrentEvent = EnumEvent.Idle;
        private readonly CRobotStatus robotStatus = new CRobotStatus();
        private double[] currentPosition = new double[15];
        private bool m_AutoStatusUpdate = false;
        private bool m_commsError = true;
        private long m_previousStatus_ms;
        private long m_commsLossTimeout = 2000;
        private int m_XRCPollInterval = 100;
        private short portNumber = 0;

        //private bool isSettingIdle = false;
        //private readonly CRobPosVar position99 = new CRobPosVar();

        // second home position  X = 1345; Y = 0; Z = 980; Rx = 180; Ry = -90; Rz = 0;  Formcode = 0; ToolNo = 0;
        // working home position X = 1345; Y = 0; Z = 980; Rx = 90;  Ry = 0;   Rz = 90; Formcode = 0; ToolNo = 0;
        // working home position pulses: S = 0, L = 0, U = 0, R = 2697, B = 0, T = 35532 
        //private readonly double[] homePosPulse = { 0, 0, 0, 2697, 0, 35532, 0, 0, 0, 0, 0, 0};
        //private readonly double[] homePosPulse = { -4, -49, -579, 2568, 201, 35594, 0, 0, 0, 0, 0, 0};
        private readonly double[] homePosPulse = { 0, 0, 0, 0, -71582, 0, 0, 0, 0, 0, 0, 0};
        
        //private readonly double[] homePosXYZ = { 1345.025, -0.028, 975.198, 90.01, 0.00, 90.01, 0, 0, 0, 0, 0, 0};
        private readonly double[] homePosXYZ = { 1170.025, 0, 805, 180, -0.01, 0, 0, 0, 0, 0, 0, 0};

        // new loading home 1170.025,0,805,180,-0.01,0 (on 27/05/2020)
        // in pulse 41 -2073 -3093 13 -71346 36784

        //private readonly CRobPosVar homePos = new CRobPosVar(FrameType.Robot, 1345.025, -0.028, 975.198, 90.01, 0.00, 90.01, 0, 0);

        // MAX X: (900, 1650) Y: (-735, 735) Z: (-1100)
        // right top: 1650, -735; left top: 1650, 735; 

        #endregion

        #region

        public event EventHandler StatusChanged;
        public event EventHandler DispatchCurrentPosition;
        public event EventHandler ConnectionStatus;
        public event EventHandler ConnectionError;
        public event EventHandler EventStatus;
        public event EventHandler MovingToPosition;

        #endregion

        #region constructor

        public MotomanConnection(short portNumber) {
            this.portNumber = portNumber;
            movingTo = new double[] {0, 0, 0, 0, 0, 0};
        }

        static MotomanConnection()
        {
            m_CommDir = Directory.GetCurrentDirectory();
            if (m_CommDir.Substring(m_CommDir.Length - 1, 1) != "\\")
            {
                m_CommDir = m_CommDir + "\\";
            }
        }
        #endregion

        #region member functions

        public void Connect()
        {
            SetConnection(EnumComms.Connecting);

            //** Initialize communication **
            short ret;

            // try to get a handle
            // m_Handle = CMotocom.BscOpen(m_CommDir, 256);
            m_Handle = CMotocom.BscOpen(m_CommDir, 1); // 1 - serial comms

            if (m_Handle >= 0)
            {
                //set IP Address
                // ret = CMotocom.BscSetEServer(m_Handle, m_IPAddress);

                ret = CMotocom.BscSetCom(m_Handle, portNumber, 9600, 2, 8, 0); //9600, Even parity, 8 data bits, 1 stop bit

                //if (ret!=1) throw new Exception("Could not set IP address !");
                if (ret != 1)
                {
                    SetError("Could not set COM port");
                    return;
                    //throw new Exception("Could not set COM port");
                }


                ret = CMotocom.BscConnect(m_Handle);
                if (ret == 0)
                {
                    SetError("Error on connecting!");
                    return;
                    //throw new Exception("Error on connecting!");
                }
                SetError("");
                SetConnection(EnumComms.Connected);
                AutoStatusUpdate = true;
            }
            else SetError("Could not get a handle. Check Hardware key!");
            //throw new Exception("Could not get a handle. Check Hardware key !");
        }

        public async Task Disconnect()
        {
            if (m_Handle < 0) return;
            
            SetConnection(EnumComms.Disconnecting);
            await TaskEx.WaitUntil(isIdle);
            CMotocom.BscDisConnect(m_Handle);
            CMotocom.BscClose(m_Handle);
            SetConnection(EnumComms.Disconnected);
        }


        private async Task ReadGeneralStatus()
        {
            await TaskEx.WaitUntil(isIdle);
            SetEvent("ReadGeneralStatus");

            short sw1 = -1, sw2 = -1;
            short ret = CMotocom.BscGetStatus(m_Handle, ref sw1, ref sw2);
            if (ret != 0) {
                SetError("Error calling BscGetStatus");
            } else
            {
                if (robotStatus.SetAndCheckSWs(sw1, sw2)) StatusChanged(this, null);

                StringBuilder framename = new StringBuilder("ROBOT"); // BASE  ROBOT
                int formCode = 0;
                short isExternal = 0;
                short toolNo = 0;
                short ret1 = CMotocom.BscGetCartPos(
                //short ret1 = CMotocom.BscIsRobotPos(
                    m_Handle, framename, isExternal, ref formCode, ref toolNo, ref currentPosition[0]);

                if (ret1 == -1)
                {
                    SetError("Error executing BscGetCartPos");
                } else
                {
                    currentPosition[13] = formCode;
                    currentPosition[14] = toolNo;

                    DispatchCurrentPosition(this, null);

                    m_previousStatus_ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }
            }
            await SetEvent(EnumEvent.Idle);
        }

        private async void StatusTimer_Tick(object sender, EventArgs e)
        {
            if (m_CurrentConnection != EnumComms.Connected || m_CurrentEvent != EnumEvent.Idle) return;
            // the timer below behaves as a timeout
            StatusTimeout.Stop();
            await ReadGeneralStatus();
            StatusTimeout.Start();
        }

        private void CheckComms(object sender, EventArgs e)
        {
            
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (now > m_previousStatus_ms + m_commsLossTimeout && !m_commsError)
            {
                m_commsError = true;
                CancelOperation();
                SetError("Communications to robot lost");
            } else if (m_commsError && now <= m_previousStatus_ms + m_commsLossTimeout)
            {
                SetConnection(EnumComms.Connected);
                m_commsError = false;
            }
        }

        public bool AutoStatusUpdate
        {
            get
            {
                return m_AutoStatusUpdate;
            }
            set
            {
                m_AutoStatusUpdate = value;
                if (m_AutoStatusUpdate)
                {
                    StatusTimeout.Interval = m_XRCPollInterval;
                    StatusTimeout.Tick += new EventHandler(StatusTimer_Tick);
                    StatusTimeout.Start();

                    CommsChecker.Interval = 2000;
                    CommsChecker.Tick += new EventHandler(CheckComms);
                    CommsChecker.Start();
                }
                else
                {
                    StatusTimeout.Stop();
                    CommsChecker.Stop();
                }
            }
        }

        private void SetError(string message)
        {
            m_Error = message;
            Console.WriteLine("ERROR: " + message);
            ConnectionError?.Invoke(this, null);
        }

        private void SetConnection(string status)
        {
            Console.WriteLine("Connection Status is " + status);
            m_CurrentConnection = status;
            ConnectionStatus?.Invoke(this, null);
        }

        private async Task SetEvent(string eventStr)
        {
            if (eventStr == EnumEvent.Idle) await Task.Delay(50);
            m_CurrentEvent = eventStr;
            EventStatus?.Invoke(this, null);
        }

        private bool isIdle()
        {
            return m_CurrentEvent == EnumEvent.Idle;
        }

        #endregion

        public double[] GetCurrentPositionCached()
        {
            return (double[])currentPosition.Clone();
        }

        public bool isOperating()
        {
            return robotStatus.isOperating;
        }

        public CRobotStatus GetCopyOfRobotStatus()
        {
            CRobotStatus cRobotStatus = new CRobotStatus();
            cRobotStatus.SetAndCheckSWs(robotStatus.sw1, robotStatus.sw2);
            return cRobotStatus;
        }









        public async void ReadByteVariable(short index, double[] posVarArray)
        {
            if (m_commsError) return;
            await TaskEx.WaitUntil(isIdle);
            SetEvent("ReadByteVariable");

            short ret = CMotocom.BscGetVarData(m_Handle, 0, index, ref posVarArray[0]);
            if (ret != 0) SetError("Error executing ReadByteVariable");

            await SetEvent(EnumEvent.Idle);
        }



        public async Task ReadPositionVariable(short Index, CRobPosVar PosVar)
        {
            if (m_commsError) return;
            await TaskEx.WaitUntil(isIdle);
            SetEvent("ReadPositionVariable");


            StringBuilder StringVal = new StringBuilder(256);
            double[] PosVarArray = new double[12];
            short ret = CMotocom.BscHostGetVarData(m_Handle, 4, Index, ref PosVarArray[0], StringVal);
            if (ret != 0) SetError("Error executing ReadPositionVariable: " + ret.ToString());
            //throw new Exception("Error executing BscHostGetVarData");
            if (PosVar != null) PosVar.HostGetVarDataArray = PosVarArray;

            await SetEvent(EnumEvent.Idle);
        }

        private async Task WritePositionVariable(short Index, CRobPosVar PosVar)
        {
            if (m_commsError) return;
            await TaskEx.WaitUntil(isIdle);
            SetEvent("WritePositionVariable");

            Console.WriteLine("X is " + PosVar.X.ToString());

            StringBuilder StringVal = new StringBuilder(256);
            double[] PosVarArray = PosVar.HostGetVarDataArray;
            short ret = CMotocom.BscHostPutVarData(m_Handle, 4, Index, ref PosVarArray[0], StringVal);
            if (ret != 0) SetError("Error executing WritePositionVariable");

            await SetEvent(EnumEvent.Idle);
        }


        //public async Task StartJob(string JobName)
        //{
        //    if (m_commsError) return;
            
        //    if (!JobName.ToLower().Contains(".jbi"))
        //    {
        //        SetError("Error *.jbi jobname extension is missing !");
        //        return;
        //    }
            
        //    await TaskEx.WaitUntil(isIdle);
        //    SetEvent("StartJob");
        //    short ret;

        //    ret = CMotocom.BscSelectJob(m_Handle, JobName);
        //    if (ret == 0)
        //    {
        //        ret = CMotocom.BscServoOn(m_Handle);
        //        if (ret != 0) SetError("Error executing BscServoON err:" + ret.ToString());
        //        else
        //        {
        //            ret = CMotocom.BscStartJob(m_Handle);
        //            if (ret != 0) SetError("Error starting job !");
        //        }
        //    }
        //    else SetError("Error selecting job !");

        //    SetEvent(EnumEvent.Idle);
        //}


        public async Task MoveToHome1(double speedSP = 20)
        {
            if (m_commsError) return;

            await TaskEx.WaitUntil(isIdle);
            SetEvent("MoveToHome1");

            await Task.Delay(50);

            short ret = 0;
            double speed = (speedSP > 30) ? 30 : ((speedSP < 0) ? 0 : speedSP);
            short toolNo = 0;

            if (IsReadyToOperate())
            {
                movingTo[0] = homePosXYZ[0];
                movingTo[1] = homePosXYZ[1];
                movingTo[2] = homePosXYZ[2];
                movingTo[3] = homePosXYZ[3];
                movingTo[4] = homePosXYZ[4];
                movingTo[5] = homePosXYZ[5];
                MovingToPosition?.Invoke(this, null);
                ret = CMotocom.BscPMovj(m_Handle, speed, toolNo, ref homePosPulse[0]);
                if (ret != 0) SetError("Error MoveToHome1:BscPMovj");
            }

            await SetEvent(EnumEvent.Idle);
        }

        public async Task MoveIncrementally(IMove iMove, double speedSP)
        {
            if (m_commsError || iMove == null) return;
            
            await TaskEx.WaitUntil(isIdle);
            SetEvent("MoveToPosition");

            await Task.Delay(50);

            if (isSafeToMove(iMove))
            {
                short ret = 0;
                StringBuilder vType = new StringBuilder("V"); // VR;
                double speed = (speedSP > 300) ? 300 : ((speedSP < 0) ? 0 : (double)speedSP); // is VJ=20.00
                StringBuilder framename = new StringBuilder("TOOL"); // ROBOT BASE
                short toolNo = 0;
                double[] iCoordinates = iMove.ToArray();

                if (IsReadyToOperate())
                {
                    MovingToPosition?.Invoke(this, null);
                    ret = CMotocom.BscImov(m_Handle, vType, speed, framename, toolNo, ref iCoordinates[0]);
                    if (ret != 0) SetError("Error MoveToPosition:BscImov");
                }
            } else SetError("Not safe to move to X=" + movingTo[0] + ", Y=" + movingTo[1] + ", Z=" + movingTo[2] + ", Rz=" + movingTo[5]);

            await SetEvent(EnumEvent.Idle);
        }

        private bool isSafeToMove(IMove iMove)
        {
            // Internal radius of the safe donut
            double Rin = 900;  // mm
            // External radius of the safe donut
            double Rex = 1700; // mm

            double currentBackForward = currentPosition[0];  // backForward
            double currentY = currentPosition[1];  // leftRight
            double currentZ = currentPosition[2];  // upDown
            double currentAcwCw = currentPosition[5];  // acwCw

            double moveToX = movingTo[0] = currentBackForward + iMove.backForward;
            double moveToY = movingTo[1] = currentY - iMove.leftRight;
            double moveToZ = movingTo[2] = currentZ - iMove.upDown;
            double rotateZ = movingTo[5] = currentAcwCw - iMove.acwCw;

            //Console.WriteLine(moveToX + ", " + moveToY + ", " + moveToZ + ", " + rotateZ);

            if (moveToZ < -1100 || moveToZ > 975) return false;
            if (rotateZ < -89.9 || rotateZ > 89.9) return false;

            double loc = moveToX * moveToX + moveToY * moveToY;
            bool outSmall = loc >= Rin * Rin;
            bool withinBig = loc <= Rex * Rex;

            if (!outSmall) SetError(Math.Round(moveToX, 2) + "," + Math.Round(moveToY, 2) + " is too close to base.");
            if (!withinBig) SetError(Math.Round(moveToX, 2) + "," + Math.Round(moveToY, 2) + " is out of reach.");

            return outSmall && withinBig;
        }

        //private bool isIncrementalMove(double[] posVar)
        //{
        //    double diff = 0;
        //    for (int i = 0; i < 6; i++)
        //    {
        //        if (posVar[i] != 0 && diff != 0) return false;
        //        if (Math.Abs(posVar[i]) > diff) diff = Math.Abs(posVar[i]);
        //    }
        //    return (diff <= 200);
        //}

        // this restricts to only one coordinate change and only within 200mm
        private bool isAtHome()
        {
                       // compare X, Y, Z, Rx, Ry, Rz
            if (Math.Round(currentPosition[0]) != Math.Round(homePosXYZ[0])) return false;
            if (Math.Round(currentPosition[1]) != Math.Round(homePosXYZ[1])) return false;
            if (Math.Round(currentPosition[2]) != Math.Round(homePosXYZ[2])) return false;
            if (Math.Round(currentPosition[3]) != Math.Round(homePosXYZ[3])) return false;
            if (Math.Round(currentPosition[4]) != Math.Round(homePosXYZ[4])) return false;
            if (Math.Round(currentPosition[5]) != Math.Round(homePosXYZ[5])) return false;
            
            return true;
        }


        private bool IsReadyToOperate()
        {
            short ret = 0;
            if (robotStatus.isServoOn)
            {
                if (robotStatus.isOperating)
                {
                    // stop any current jobs
                    ret = CMotocom.BscHoldOn(m_Handle);
                    if (ret != 0)
                    {
                        SetError("Error IsReadyToOperate:BscHoldOn");
                        return false;
                    }
                    else
                    {
                        ret = CMotocom.BscHoldOff(m_Handle);
                        if (ret != 0)
                        {
                            SetError("Error IsReadyToOperate:BscHoldOff");
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                SetError("Error MoveToPosition: SERVO is OFF");
                return false;
            }
        }

        //private async Task MoveToPosition(double[] posVar, decimal speedSP)
        //{

        //    await TaskEx.WaitUntil(isIdle);
        //    SetEvent("MoveToPosition");
        //    short ret = 0;

        //    StringBuilder vType = new StringBuilder("V"); // VR;
        //    double speed = (speedSP > 300) ? 300 : ((speedSP < 0) ? 0 : (double)speedSP); // is VJ=20.00
        //    StringBuilder framename = new StringBuilder("TOOL"); // ROBOT BASE
        //    short toolNo = 0;

        //    double[] pos = (posVar != null) ? posVar : new double[]{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        //    //if (posVar != null) pos = new double[] { posVar.X, posVar.Y, posVar.Z, posVar.Rx, posVar.Ry, posVar.Rz, 0, 0, 0, 0, 0, 0 };

        //    //short form = (short)Convert.ToInt32("0", 2); ;  // Bits: 0 No-flip, 1 elbow under, 2 Back side, 3 R>=180, 4 T>=180, S>=180

        //    if (IsReadyToOperate())
        //    {
        //        ret = CMotocom.BscImov(m_Handle, vType, speed, framename, toolNo, ref pos[0]);
        //        if (ret != 0) SetError("Error MoveToPosition:BscMovj");
        //    }

        //    SetError("NOT ERROR: finished moving.");
        //    SetEvent(EnumEvent.Idle);

        //    //void moveRobot() {
        //    //    ret = (isHome) ?
        //    //            CMotocom.BscPMovj(m_Handle, 30, toolNo, ref homePosPulse[0]) :
        //    //            CMotocom.BscImov(m_Handle, vType, speed, framename, toolNo, ref pos[0]);
        //    //    if (ret != 0) SetError("Error MoveToPosition:BscMovj");
        //    //}
        //}

        public async Task CancelOperation()
        {
            await TaskEx.WaitUntil(isIdle);
            SetEvent("CancelOperation");

            short ret = 0;
            ret = CMotocom.BscHoldOn(m_Handle);
            if (ret != 0) SetError("Error CancelOperation:BscHoldOn");
            else
            {
                ret = CMotocom.BscHoldOff(m_Handle);
                if (ret != 0) SetError("Error CancelOperation:BscHoldOff");
            }
            await SetEvent(EnumEvent.Idle);
        }


        public async Task MoveByJob(bool isHome, CRobPosVar position = null)
        {
            if (isHome)
            {
                await MoveToHome1();
                return;
            }

            if (position == null) return;

            await TaskEx.WaitUntil(isIdle);
            SetEvent("MoveByJob");
            short ret = 0;
            string moveHomeJob = "TO_POS_0.jbi";
            double[] speed = new double[10];
            speed[0] = 2000; // is VJ=20.00
            short varNo = 0;

            //// stop any current jobs
            //ret = CMotocom.BscHoldOn(m_Handle);
            //if (ret != 0) SetError("Error MoveToPosition:BscHoldOn");
            //else
            //{
                // write position at varNo
                StringBuilder StringVal = new StringBuilder(256);
                double[] PosVarArray = position.HostGetVarDataArray;
                ret = CMotocom.BscHostPutVarData(m_Handle, 4, varNo, ref PosVarArray[0], StringVal);
                if (ret != 0) SetError("Error executing MoveToPosition:WritePositionVariable");
                else
                {
                    // read position at varNo
                    StringVal = new StringBuilder(256);
                    double[] readPosition = new double[10];
                    ret = CMotocom.BscHostGetVarData(m_Handle, 4, varNo, ref readPosition[0], StringVal);
                    if (ret != 0) SetError("Error executing MoveToPosition:ReadPositionVariable");
                    else
                    {
                        // compare the set home position with read from robot
                        if (!readPosition.SequenceEqual(position.HostGetVarDataArray)) SetError("MoveToPosition: arrays are not equal.");
                        else
                        {
                            ret = CMotocom.BscPutVarData(m_Handle, 1, varNo, ref speed[0]);
                            if (ret != 0) SetError("Error executing MoveToPosition:ReadByteVariable");
                            else
                            {
                                // select job
                                ret = CMotocom.BscSelectJob(m_Handle, moveHomeJob);
                                if (ret != 0) SetError("Error selecting job at MoveToPosition");
                                else
                                {
                                    // run the job
                                    ret = CMotocom.BscStartJob(m_Handle);
                                    if (ret != 0) SetError("Error starting job at MoveToPosition!");
                                }
                            }
                        }
                    }
                }
            //}

            await SetEvent(EnumEvent.Idle);
        }













        #region Properties
        public static string CommDir
        {
            get { return MotomanConnection.m_CommDir; }
            set { MotomanConnection.m_CommDir = value; }
        }

        public string CurrentConnection
        {
            get { return m_CurrentConnection; }
        }

        public string CurrentEvent
        {
            get { return m_CurrentEvent; }
        }
        
        public string CurrentError
        {
            get { return m_Error; }
        }

        public string RobotStatusJson
        {
            get { return JsonConvert.SerializeObject(robotStatus);  }
        }

        public bool CommsError { get => m_commsError; }

        public double[] HomePosDataArray
        {
            get {
                double[] posCopy = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
                Array.Copy(homePosXYZ, posCopy, homePosXYZ.Length);
                return posCopy;
            }
        }

        public double[] movingTo { get; internal set; }

        #endregion
    }
}
