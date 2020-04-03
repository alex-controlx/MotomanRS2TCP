using Newtonsoft.Json;
using System;
using System.Windows.Forms;

namespace MotomanRS2TCP
{
    public partial class Form1 : Form
    {
        private delegate void SafeCallDelegate(string text);
        private delegate void Label9Delegate(object sender, EventArgs e);
        private NodeTcpListener server;
        private MotomanConnection xrc;
        private int uiUpdateCounter = 0;
        private readonly CRobPosVar posSP = new CRobPosVar();
        private readonly CRobPosVar homePos = new CRobPosVar();
        private readonly short varIndex = 0;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            label7.Text = uiUpdateCounter.ToString();

            homePos.X = 1135;
            homePos.Y = -145;
            homePos.Z = 1060;
            homePos.Rx = 90;
            homePos.Ry = 4;
            homePos.Rz = 82;
            homePos.Formcode = 0;
            homePos.ToolNo = 0;

            UpdateUiCurrentPosition();
            UpdateUiSetpointPosition();
            StartApp();
        }


        private void Form1_FormClosing(object sender, EventArgs e)
        {
            if (xrc != null)
            {
                xrc.Disconnect();
                xrc = null;
                Console.WriteLine("Stopping XRC connection");
            }
            Console.WriteLine("Stopping TCP server");
            if (server != null)  server.StopServer();
        }


        private void StartApp()
        {
            try
            {
                xrc = new MotomanConnection();
                xrc.StatusChanged += new EventHandler(StatusChanged); // Register Eventhandler for status change
                xrc.ConnectionStatus += new EventHandler(rc1_connectionStatus); // Register Eventhandler for status change
                xrc.EventStatus += new EventHandler(rc1_eventStatus); // Register Eventhandler for status change
                xrc.ConnectionError += new EventHandler(rc1_errorStatus); // Register Eventhandler for status change
                xrc.DispatchCurrentPosition += new EventHandler(
                    (object sender, EventArgs e) => { UpdateUiCurrentPosition(); }
                );

                WriteLine("XRC Starting connection");
                xrc.Connect();
                //xrc.AutoStatusUpdate = true;

                WriteLine("TCP Starting server");
                server = new NodeTcpListener(this, xrc, 4305);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void rc1_connectionStatus(object sender, EventArgs e)
        {
            WriteLine("    XRC Connection: " + xrc.CurrentConnection);
        }

        private void rc1_eventStatus(object sender, EventArgs e)
        {
            //WriteLine("    XRC Event: " + xrc.CurrentEvent);
            // label9.Text = xrc.CurrentEvent;

            if (label9.InvokeRequired)
            {
                label9.Invoke(new Label9Delegate(rc1_eventStatus), new object[] { sender, e });
            }
            else
            {
                label9.Text = xrc.CurrentEvent;

            }
        }

        private void rc1_errorStatus(object sender, EventArgs e)
        {
            WriteLine("    XRC Error: " + xrc.CurrentError);
        }

        private void StatusChanged(object sender, EventArgs e)
        {
            WriteLine("    XRC Status: " + xrc.RobotStatusJson);
        }

        public void WriteLine(string message)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new SafeCallDelegate(WriteLine), new object[] { message });
            } else
            {
                listBox1.Items.Add(message);
                if (listBox1.Items.Count > 100)
                {
                    listBox1.Items.RemoveAt(0);
                }

                // Make sure the last item is made visible
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.ClearSelected();

            }
        }


        private void btnUp_Click(object sender, EventArgs e)
        {
            posSP.X = posSP.X + 10;
            UpdateUiSetpointPosition();
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            posSP.X = posSP.X - 10;
            UpdateUiSetpointPosition();
        }

        private void btnHomePos_Click(object sender, EventArgs e)
        {
            Array.Copy(homePos.HostGetVarDataArray, posSP.HostGetVarDataArray, homePos.HostGetVarDataArray.Length);
            UpdateUiSetpointPosition();
        }

        private void btnGetPos_Click(object sender, EventArgs e)
        {
            //getGeneralStatus();
            //AutomaticStatusRead();
        }

        private void GetPositionVariable()
        {
            //if (xrc.CurrentEvent != EnumEvent.Idle) return;
            CRobPosVar posVar = new CRobPosVar();
            xrc.ReadPositionVariable(varIndex, posVar);
            UpdateUiPositionVariable(posVar);
        }

        private Timer statusReadTimeout = new Timer();
        //private void AutomaticStatusRead()
        //{
            
        //    getGeneralStatus();
            
        //}

        private void getGeneralStatus()
        {
            if (xrc.CurrentEvent != EnumEvent.Idle) return;
            
            UpdateUiCurrentPosition();
        }
        


        private void UpdateUiPositionVariable(CRobPosVar posVar)
        {
            uiUpdateCounter++;
            label7.Text = uiUpdateCounter.ToString();
            if (posVar != null)
            {
                if (posVar.DataType == PosVarType.XYZ)
                {
                    label5.Text = "X:" + posVar.X.ToString() + " " +
                        "Y:" + posVar.Y.ToString() + " " +
                        "Z:" + posVar.Z.ToString() + " " +
                        "Rx:" + posVar.Rx.ToString() + " " +
                        "Ry:" + posVar.Ry.ToString() + " " +
                        "Rz:" + posVar.Rz.ToString() + " " +
                        "F:" + posVar.Formcode.ToString() + " " +
                        "Tool:" + posVar.ToolNo.ToString();
                }
                else label5.Text = "Not XYZ coordinates";
            }
        }


        private void UpdateUiCurrentPosition()
        {
            if (xrc == null) return;

            var currentPosition = xrc.GetCurrentPosition();
            label6.Text = "X:" + currentPosition[0].ToString() + " " +
                "Y:" + currentPosition[1].ToString() + " " +
                "Z:" + currentPosition[2].ToString() + " " +
                "Rx:" + currentPosition[3].ToString() + " " +
                "Ry:" + currentPosition[4].ToString() + " " +
                "Rz:" + currentPosition[5].ToString() + " " +
                "F:" + currentPosition[13].ToString() + " " +
                "Tool:" + currentPosition[14].ToString();
        }

        private void UpdateUiSetpointPosition()
        {
            label4.Text = "X:" + posSP.X.ToString() + " " +
                "Y:" + posSP.Y.ToString() + " " +
                "Z:" + posSP.Z.ToString() + " " +
                "Rx:" + posSP.Rx.ToString() + " " +
                "Ry:" + posSP.Ry.ToString() + " " +
                "Rz:" + posSP.Rz.ToString() + " " +
                "F:" + posSP.Formcode.ToString() + " " +
                "Tool:" + posSP.ToolNo.ToString();
        }


        private void readByteVariableExample()
        {
            try
            {
                double[] doubles = new double[10];
                xrc.ReadByteVariable(0, doubles);
                WriteLine("    Byte var: " + doubles[0].ToString());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void btnCurrentPos2SP_Click(object sender, EventArgs e)
        {
            if (xrc == null) return;

            var currentPosition = xrc.GetCurrentPosition();
            posSP.X = currentPosition[0];
            posSP.Y = currentPosition[1];
            posSP.Z = currentPosition[2];
            posSP.Rx = currentPosition[3];
            posSP.Ry = currentPosition[4];
            posSP.Rz = currentPosition[5];
            posSP.Formcode = Convert.ToInt16(currentPosition[13]);
            posSP.ToolNo = Convert.ToInt16(currentPosition[14]);
            UpdateUiSetpointPosition();
        }

        private void btnSetPosVar_Click(object sender, EventArgs e)
        {
            xrc.WritePositionVariable(varIndex, posSP);
            GetPositionVariable();
        }

        private void btnGetPosVar_Click(object sender, EventArgs e)
        {
            GetPositionVariable();
        }
    }
}
