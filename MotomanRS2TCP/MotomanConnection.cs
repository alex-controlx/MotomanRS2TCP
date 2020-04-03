using System;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MotomanRS2TCP
{

    public class MotomanConnection
    {
        #region member variables

        private short m_Handle = -1;
        Timer StatusTimer = new Timer();
        private static Object m_FileAccessDirLock = new Object();
        private Object m_YasnacAccessLock = new Object();
        private static string m_CommDir;
        private string m_CurrentConnection = EnumComms.Disconnected;
        private string m_Error = "";
        private string m_CurrentEvent = EnumEvent.Idle;
        private readonly CRobotStatus robotStatus = new CRobotStatus();
        private double[] currentPosition = new double[15];
        private bool m_AutoStatusUpdate = false;

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

        public void Disconnect()
        {
            if (m_Handle >= 0)
            {
                SetConnection(EnumComms.Disconnecting);
                CMotocom.BscDisConnect(m_Handle);
                CMotocom.BscClose(m_Handle);
                SetConnection(EnumComms.Disconnected);
            }
        }


        private void ReadGeneralStatus()
        {
            lock (m_YasnacAccessLock)
            {
                SetEvent("getGeneralStatus");

                short sw1 = -1, sw2 = -1;
                Console.WriteLine("Reading BscGetStatus 1");
                short ret = CMotocom.BscGetStatus(m_Handle, ref sw1, ref sw2);
                if (ret != 0) {
                    Console.WriteLine("Error calling BscGetStatus in getGeneralStatus: " + ret.ToString());
                    SetError("Error calling BscGetStatus in getGeneralStatus");
                    return;
                }
                if (robotStatus.SetAndCheckSWs(sw1, sw2)) StatusChanged(this, null);

                Console.WriteLine("Reading BscGetStatus 2");

                StringBuilder framename = new StringBuilder("ROBOT");
                int formCode = 0;
                short isExternal = 0;
                short toolNo = 0;
                short ret1 = CMotocom.BscGetCartPos(
                    m_Handle, framename, isExternal, ref formCode, ref toolNo, ref currentPosition[0]
                    );
                if (ret1 == -1) SetError("Error executing BscDCIGetPos");
                currentPosition[13] = formCode;
                currentPosition[14] = toolNo;
                Console.WriteLine("Reading BscGetCartPos: " + ret1.ToString());

                DispatchCurrentPosition(this, null);

                StatusTimer.Start();
                SetEvent(EnumEvent.Idle);
            }
        }

        public void ReadByteVariable(short index, double[] posVarArray)
        {
            lock (m_YasnacAccessLock)
            {
                SetEvent("ReadByteVariable");

                short ret = CMotocom.BscGetVarData(m_Handle, 0, index, ref posVarArray[0]);
                if (ret != 0) SetError("Error executing ReadByteVariable");
                
                SetEvent(EnumEvent.Idle);
            }
            
        }



        public void ReadPositionVariable(short Index, CRobPosVar PosVar)
        {
            lock (m_YasnacAccessLock)
            {
                SetEvent("ReadPositionVariable");

                StringBuilder StringVal = new StringBuilder(256);
                double[] PosVarArray = new double[12];
                short ret = CMotocom.BscHostGetVarData(m_Handle, 4, Index, ref PosVarArray[0], StringVal);
                if (ret != 0) SetError("Error executing ReadPositionVariable: " + ret.ToString());
                //throw new Exception("Error executing BscHostGetVarData");
                if (PosVar != null) PosVar.HostGetVarDataArray = PosVarArray;

                SetEvent(EnumEvent.Idle);
            }
        }

        public void WritePositionVariable(short Index, CRobPosVar PosVar)
        {
            lock (m_YasnacAccessLock)
            {
                SetEvent("WritePositionVariable");

                StringBuilder StringVal = new StringBuilder(256);
                double[] PosVarArray = PosVar.HostGetVarDataArray;
                short ret = CMotocom.BscHostPutVarData(m_Handle, 4, Index, ref PosVarArray[0], StringVal);
                if (ret != 0) SetError("Error executing WritePositionVariable");

                SetEvent(EnumEvent.Idle);
            }
        }

        private void TestFunc()
        {
            Console.WriteLine("TestFunc called");
        }

        List<Action> m_functions = new List<Action>();
        Action m_function;
        public void WritePositionVariable2(short Index, CRobPosVar PosVar)
        {
            if (!m_functions.Contains(TestFunc)) m_functions.Add(TestFunc);
        }






        public double[] GetCurrentPosition()
        {
            return (double[])currentPosition.Clone();
        }


        void StatusTimer_Tick(object sender, EventArgs e)
        {

            Console.WriteLine("     >>>>>>>>>     StatusTimer_Tick 1");
            if (m_CurrentConnection != EnumComms.Connected || m_CurrentEvent != EnumEvent.Idle) return;
            Console.WriteLine("     >>>>>>>>>     StatusTimer_Tick 2");
            StatusTimer.Stop();
            ReadGeneralStatus();            
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
            if (ConnectionError != null) ConnectionError(this, null);
        }

        private void SetConnection(string status)
        {
            Console.WriteLine("Connection Status is " + status);
            m_CurrentConnection = status;
            if (ConnectionStatus != null) ConnectionStatus(this, null);
        }

        private void SetEvent(string eventStr)
        {
            m_CurrentEvent = eventStr;
            if (EventStatus != null) EventStatus(this, null);
        }

        #endregion





















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

        #endregion
    }
}
