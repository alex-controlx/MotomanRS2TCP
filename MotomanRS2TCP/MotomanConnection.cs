using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MotomanRS2TCP
{

    public class MotomanConnection
    {
        #region member variables

        private short m_Handle = -1;
        System.Windows.Forms.Timer StatusTimer = new System.Windows.Forms.Timer();
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

        #endregion

        #region

        public event EventHandler StatusChanged;
        public event EventHandler DispatchCurrentPosition;
        public event EventHandler ConnectionStatus;
        public event EventHandler ConnectionError;
        public event EventHandler EventStatus;
        
        #endregion

        #region constructor

        public MotomanConnection() {}

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
            }
            else SetError("Could not get a handle. Check Hardware key!");
            //throw new Exception("Could not get a handle. Check Hardware key !");
        }

        public async void Disconnect()
        {
            if (m_Handle >= 0)
            {
                SetConnection(EnumComms.Disconnecting);
                await TaskEx.WaitUntil(isIdle);
                CMotocom.BscDisConnect(m_Handle);
                CMotocom.BscClose(m_Handle);
                SetConnection(EnumComms.Disconnected);
            }
        }


        private async void ReadGeneralStatus()
        {
            await TaskEx.WaitUntil(isIdle);
            SetEvent("ReadGeneralStatus");

            short sw1 = -1, sw2 = -1;
            short ret = CMotocom.BscGetStatus(m_Handle, ref sw1, ref sw2);
            if (ret != 0) {
                Console.WriteLine("Error calling BscGetStatus in getGeneralStatus: " + ret.ToString());
                SetError("Error calling BscGetStatus in getGeneralStatus");
                m_commsError = true;

            } else
            {
                if (robotStatus.SetAndCheckSWs(sw1, sw2)) StatusChanged(this, null);

                StringBuilder framename = new StringBuilder("ROBOT");
                int formCode = 0;
                short isExternal = 0;
                short toolNo = 0;
                short ret1 = CMotocom.BscGetCartPos(
                    m_Handle, framename, isExternal, ref formCode, ref toolNo, ref currentPosition[0]);

                if (ret1 == -1)
                {
                    SetError("Error executing BscDCIGetPos");
                    m_commsError = true;
                } else
                {
                    currentPosition[13] = formCode;
                    currentPosition[14] = toolNo;
                    m_commsError = false;

                    DispatchCurrentPosition(this, null);
                }
            }
            SetEvent(EnumEvent.Idle);
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



        void StatusTimer_Tick(object sender, EventArgs e)
        {
            if (m_CurrentConnection != EnumComms.Connected || m_CurrentEvent != EnumEvent.Idle) return;
            // the timer below behaves as a timeout
            StatusTimer.Stop();
            ReadGeneralStatus();
            StatusTimer.Start();
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
                    StatusTimer.Interval = 2000;
                    StatusTimer.Tick += new EventHandler(StatusTimer_Tick);
                    StatusTimer.Start();

                }
                else StatusTimer.Stop();
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
        }

        private bool isIdle()
        {
            return m_CurrentEvent == EnumEvent.Idle;
        }

        #endregion


        public double[] GetCurrentPosition()
        {
            return (double[])currentPosition.Clone();
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

        #endregion
    }
}
