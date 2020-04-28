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
        private bool isSettingIdle = false;
        private readonly CRobPosVar position99 = new CRobPosVar();

        // second home position  X = 1345; Y = 0; Z = 980; Rx = 180; Ry = -90; Rz = 0;  Formcode = 0; ToolNo = 0;
        // working home position X = 1345; Y = 0; Z = 980; Rx = 90;  Ry = 0;   Rz = 90; Formcode = 0; ToolNo = 0;
        // working home position pulses: S = 0, L = 0, U = 0, R = 2697, B = 0, T = 35532 
        private readonly double[] homePosPulse = { 0, 0, 0, 2697, 0, 35532, 0, 0, 0, 0, 0, 0};
        private readonly CRobPosVar homePos = new CRobPosVar(FrameType.Robot, 1345, 0, 980, 90, 0, 90, 0, 0);

        #endregion

        #region

        public event EventHandler StatusChanged;
        public event EventHandler DispatchCurrentPosition;
        public event EventHandler ConnectionStatus;
        public event EventHandler ConnectionError;
        public event EventHandler EventStatus;

        #endregion

        #region constructor

        public MotomanConnection() { }

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

                ret = CMotocom.BscSetCom(m_Handle, 3, 9600, 2, 8, 0); //9600, Even parity, 8 data bits, 1 stop bit

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
            SetEvent(EnumEvent.Idle);
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
            ConnectionError?.Invoke(this, null);
        }

        private void SetConnection(string status)
        {
            Console.WriteLine("Connection Status is " + status);
            m_CurrentConnection = status;
            ConnectionStatus?.Invoke(this, null);
        }

        private void SetEvent(string eventStr)
        {
            m_CurrentEvent = eventStr;
            EventStatus?.Invoke(this, null);
            //if (eventStr == EnumEvent.Idle && isSettingIdle) return;
            //if (eventStr != EnumEvent.Idle && isSettingIdle) throw new Exception("Cannot set Event when setting Idle");

            //if (eventStr == EnumEvent.Idle)
            //{
            //    isSettingIdle = true;
            //    await Task.Delay(100);
            //    m_CurrentEvent = eventStr;
            //    EventStatus?.Invoke(this, null);
            //    isSettingIdle = false;
            //} else
            //{
            //    m_CurrentEvent = eventStr;
            //    EventStatus?.Invoke(this, null);
            //}
        }

        private bool isIdle()
        {
            return m_CurrentEvent == EnumEvent.Idle;
        }

        #endregion

        public double[] GetCurrentPositionCached()
        {
            // posSP.X = currentPosition[0];
            // posSP.Y = currentPosition[1];
            // posSP.Z = currentPosition[2];
            // posSP.Rx = currentPosition[3];
            // posSP.Ry = currentPosition[4];
            // posSP.Rz = currentPosition[5];
            // posSP.Formcode = Convert.ToInt16(currentPosition[13]);
            // posSP.ToolNo = Convert.ToInt16(currentPosition[14]);
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

            SetEvent(EnumEvent.Idle);
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

            SetEvent(EnumEvent.Idle);
        }

        public async Task WritePositionVariable(short Index, CRobPosVar PosVar)
        {
            if (m_commsError) return;
            await TaskEx.WaitUntil(isIdle);
            SetEvent("WritePositionVariable");

            Console.WriteLine("X is " + PosVar.X.ToString());

            StringBuilder StringVal = new StringBuilder(256);
            double[] PosVarArray = PosVar.HostGetVarDataArray;
            short ret = CMotocom.BscHostPutVarData(m_Handle, 4, Index, ref PosVarArray[0], StringVal);
            if (ret != 0) SetError("Error executing WritePositionVariable");

            SetEvent(EnumEvent.Idle);
        }


        public async Task StartJob(string JobName)
        {
            if (m_commsError) return;
            
            if (!JobName.ToLower().Contains(".jbi"))
            {
                SetError("Error *.jbi jobname extension is missing !");
                return;
            }
            
            await TaskEx.WaitUntil(isIdle);
            SetEvent("StartJob");
            short ret;

            ret = CMotocom.BscSelectJob(m_Handle, JobName);
            if (ret == 0)
            {
                ret = CMotocom.BscServoOn(m_Handle);
                if (ret != 0) SetError("Error executing BscServoON err:" + ret.ToString());
                else
                {
                    ret = CMotocom.BscStartJob(m_Handle);
                    if (ret != 0) SetError("Error starting job !");
                }
            }
            else SetError("Error selecting job !");

            SetEvent(EnumEvent.Idle);
        }

        public async Task MoveToPosition(bool isHome, decimal speedSP,  CRobPosVar posVar = null)
        {
            if (!isHome && posVar == null) return;

            await TaskEx.WaitUntil(isIdle);
            SetEvent("MoveToPosition");
            short ret = 0;
            double speed = (speedSP > 30) ? 30 : ((speedSP < 0) ? 0 : (double)speedSP); // is VJ=20.00


            StringBuilder framename = new StringBuilder("ROBOT"); // ROBOT BASE
            StringBuilder vType = new StringBuilder("V"); // VR;
            double[] pos = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (posVar != null) pos = new double[] { posVar.X, posVar.Y, posVar.Z, posVar.Rx, posVar.Ry, posVar.Rz, 0, 0, 0, 0, 0, 0 };
            // 90.01, 0.00, 90.08 
            if (pos[3] < 90.01) pos[3] = 90.01;
            //if (pos[3] < 90.01) pos[3] = 90.01;
            if (pos[5] < 90.08) pos[5] = 90.08;


            short form = (short)Convert.ToInt32("0", 2); ;  // Bits: 0 No-flip, 1 elbow under, 2 Back side, 3 R>=180, 4 T>=180, S>=180
            short toolNo = 0;

            if (robotStatus.isServoOn)
            {
                if (robotStatus.isOperating)
                {
                    // stop any current jobs
                    ret = CMotocom.BscHoldOn(m_Handle);
                    if (ret != 0) SetError("Error MoveToPosition:BscHoldOn");
                    else
                    {
                        ret = CMotocom.BscHoldOff(m_Handle);
                        if (ret != 0) SetError("Error MoveToPosition:BscHoldOff");
                        else
                        {
                            moveRobot();
                        }
                    }
                }
                else
                {
                    moveRobot();
                }
            }
            else
            {
                SetError("Error MoveToPosition: SERVO is OFF");
            }

            SetError("NOT ERROR: finished moving.");
            SetEvent(EnumEvent.Idle);

            void moveRobot() {
                ret = (isHome) ?
                        CMotocom.BscPMovj(m_Handle, speed, toolNo, ref homePosPulse[0]) :
                        CMotocom.BscMovl(m_Handle, vType, speed, framename, form,toolNo, ref pos[0]);
                if (ret != 0) SetError("Error MoveToPosition:BscMovj");
            }
        }

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
            SetEvent(EnumEvent.Idle);
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
            get { return homePos.HostGetVarDataArray; }
        }

        #endregion
    }
}
