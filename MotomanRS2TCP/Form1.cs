using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotomanRS2TCP
{
    public partial class Form1 : Form
    {
        private delegate void SafeCallDelegate(string text);
        private delegate void Label9Delegate(object sender, EventArgs e);
        private delegate void Label5Delegate(CRobPosVar posVar);
        private delegate void Label6Delegate();
        private NodeSocketIO ioClient;
        private MotomanConnection xrc;
        private int uiUpdateCounter = 0;
        private decimal speedSP;
        private bool isCycling = false;
        private readonly CRobPosVar posSP = new CRobPosVar();
        private readonly short varIndex = 0;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            label7.Text = uiUpdateCounter.ToString();

            speedSP = 200;
            numericUpDown1.Value = speedSP;

            StartApp();
        }


        private async void Form1_FormClosing(object sender, EventArgs e)
        {

            if (ioClient != null)
            {
                Console.WriteLine("Stopping Socket IO");
                await ioClient.Disconnect();
            }
            if (xrc != null)
            {
                await xrc.Disconnect();
                xrc = null;
                Console.WriteLine("Stopping XRC connection");
            }
        }


        private void StartApp()
        {
            try
            {
                xrc = new MotomanConnection();
                ioClient = new NodeSocketIO(xrc);

                xrc.StatusChanged += new EventHandler(StatusChanged);
                xrc.ConnectionStatus += new EventHandler(rc1_connectionStatus);
                xrc.EventStatus += new EventHandler(rc1_eventStatus);
                xrc.ConnectionError += new EventHandler(rc1_errorStatus);
                xrc.DispatchCurrentPosition += new EventHandler(
                    (object sender, EventArgs e) => { UpdateUiCurrentPosition(); }
                );

                // copy Home Position to Setpoint array
                Array.Copy(xrc.HomePosDataArray, posSP.HostGetVarDataArray, posSP.HostGetVarDataArray.Length);

                WriteLine("XRC Starting connection");
                xrc.Connect();

                WriteLine("Starting Socket IO");
                ioClient.Connect();

                UpdateUiSetpointPosition();
            } catch (Exception ex)
            {
                MessageBox.Show("StartApp(): " + ex.Message);
            }
        }

        private void rc1_connectionStatus(object sender, EventArgs e)
        {
            if (xrc == null) return;
            WriteLine("    XRC Connection: " + xrc.CurrentConnection);
        }

        private void rc1_eventStatus(object sender, EventArgs e)
        {
            if (xrc == null) return;
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
            if (xrc == null) return;
            WriteLine("    XRC Error: " + xrc.CurrentError);
        }

        private void StatusChanged(object sender, EventArgs e)
        {
            if (xrc == null) return;
            
            ioClient.SendStatus();
            WriteLine("    XRC Status: " + xrc.RobotStatusJson);

            CRobotStatus status = xrc.GetCopyOfRobotStatus();
            if (status.isServoOn)
            {
                btnUp.Enabled = true;
                btnDown.Enabled = true;
                button1.Enabled = true;
                btnHomePos.Enabled = true;
            } else
            {
                btnUp.Enabled = false;
                btnDown.Enabled = false;
                button1.Enabled = false;
                btnHomePos.Enabled = false;
            }
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

        private void btnHomePos_Click(object sender, EventArgs e)
        {
            xrc.MoveToHome1();
        }

        private void UpdateUiPositionVariable(CRobPosVar posVar)
        {
            uiUpdateCounter++;
            if (label5.InvokeRequired)
            {
                label5.Invoke(new Label5Delegate(UpdateUiPositionVariable), new object[] { posVar });
            }
            else
            {
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
        }


        private void UpdateUiCurrentPosition()
        {
            if (xrc == null) return;

            var currentPosition = xrc.GetCurrentPositionCached();
            ioClient.SendPosition(currentPosition);
            if (label6.InvokeRequired)
            {
                label6.Invoke(new Label6Delegate(UpdateUiCurrentPosition));
            }
            else
            {
                label6.Text = "X:" + currentPosition[0].ToString() + " " +
                    "Y:" + currentPosition[1].ToString() + " " +
                    "Z:" + currentPosition[2].ToString() + " " +
                    "Rx:" + currentPosition[3].ToString() + " " +
                    "Ry:" + currentPosition[4].ToString() + " " +
                    "Rz:" + currentPosition[5].ToString() + " " +
                    "F:" + currentPosition[13].ToString() + " " +
                    "Tool:" + currentPosition[14].ToString();
            }
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


        private void btnCurrentPos2SP_Click(object sender, EventArgs e)
        {
            if (xrc == null) return;

            var currentPosition = xrc.GetCurrentPositionCached();
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

        private async void btnGetPosVar_Click(object sender, EventArgs e)
        {
            CRobPosVar posVar = new CRobPosVar();
            await xrc.ReadPositionVariable(varIndex, posVar);
            UpdateUiPositionVariable(posVar);
        }

        private void readByteVariableExample()
        {
            double[] doubles = new double[10];
            xrc.ReadByteVariable(0, doubles);
            WriteLine("    Byte var: " + doubles[0].ToString());
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await xrc.MoveByJob(false, posSP);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            speedSP = numericUpDown1.Value;
        }

        private bool isOperating() { return xrc.isOperating(); }
        private bool isNotOperating() { return !xrc.isOperating(); }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            isCycling = !isCycling;
            if (isCycling) button2.Text = "Stop Cycle";
            else button2.Text = "Start Cycle";

            if (!isCycling) await xrc.CancelOperation();

            button2.Enabled = true;
            while (isCycling)
            {
                double[] posA = { -600, -1500, 200, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                await xrc.MoveToPosition(posA, (double)speedSP);
                await TaskEx.WaitUntil(isOperating);
                await TaskEx.WaitUntil(isNotOperating);
                await Task.Delay(200);

                await xrc.MoveToHome1();
                await TaskEx.WaitUntil(isOperating);
                await TaskEx.WaitUntil(isNotOperating);
                await Task.Delay(200);

                if (!isCycling) return;

                double[] posB = { 800, -1600, -400, 0, 85, 0, 0, 0, 0, 0, 0, 0 };
                await xrc.MoveToPosition(posB, (double)speedSP);
                await TaskEx.WaitUntil(isOperating);
                await TaskEx.WaitUntil(isNotOperating);
                await Task.Delay(200);

                await xrc.MoveToHome1();
                await TaskEx.WaitUntil(isOperating);
                await TaskEx.WaitUntil(isNotOperating);
                await Task.Delay(200);
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            // UP
            ShiftRobit(200, 0, 0, 0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // DOWN
            ShiftRobit(0, 200, 0, 0);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // RIGHT
            ShiftRobit(0, 0, 200, 0);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // LEFT
            ShiftRobit(0, 0, 0, 200);
        }

        private void ShiftRobit(double up, double down, double right,  double left)
        {
            if (xrc == null) return;
            double[] newPos = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            newPos[0] = newPos[0] + left - right;
            newPos[1] = newPos[1] + up - down;
            //newPos[2] = newPos[2] + fwd - bck;

            //var currentPosition = xrc.GetCurrentPositionCached();
            //CRobPosVar newPos = new CRobPosVar();
            //newPos.Frame = FrameType.Robot;
            //newPos.X = currentPosition[0];
            //newPos.Y = currentPosition[1] + right - left;
            //newPos.Z = currentPosition[2] + up - down;
            //newPos.Rx = currentPosition[3];
            //newPos.Ry = currentPosition[4];
            //newPos.Rz = currentPosition[5];
            //newPos.Formcode = Convert.ToInt16(currentPosition[13]);
            //newPos.ToolNo = Convert.ToInt16(currentPosition[14]);
            xrc.MoveToPosition(newPos, (double)speedSP);
        }

        private async void btnUp_Click(object sender, EventArgs e)
        {
            // Position A
            //  1453.801, -787.187, -258.498, 88.66, -0.03, 87.77

            if (xrc == null) return;

            // 1345.025, -0.028, 975.198, 90.01, 0.00, 90.01 => MAX X: (900, 1650) Y: (-735, 735) Z: (-1100)
            //  | +-735  |  -2000,0  |  -445,305   |  0  |  +-89    |  0
            //  |  -R+L  |  -Dn  +Up |  -Bck +Fwd  |  0  | -ACW+CW  |  0
            double[] posA = { -600, -1500, 200, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            await xrc.MoveToPosition(posA, (double)speedSP);

            //posSP.X = posSP.X + 10;
            //UpdateUiSetpointPosition();
        }

        private async void btnDown_Click(object sender, EventArgs e)
        {
            // Position B
            //  1247.378, 668.79, -407.418, 88.67, -0.05, -178.18
            if (xrc == null) return;
            //CRobPosVar posB = new CRobPosVar(FrameType.Robot, 1247.378, 668.79, -407.418, 90.01, 0.00, -175, 0, 0);
            //xrc.MoveByJob(false, speedSP, posB);

            // 1345.025, -0.028, 975.198, 90.01, 0.00, 90.01 => MAX X: (900, 1650) Y: (-735, 735) Z: (-1100)
            //  | +-735  |  -2000,0  |  -445,305   |  0  |  +-89    |  0
            //  |  -R+L  |  -Dn  +Up |  -Bck +Fwd  |  0  | -CW+ACW  |  0
            double[] posB = { 800, -1500, -400, 0, 85, 0, 0, 0, 0, 0, 0, 0 };
            await xrc.MoveToPosition(posB, (double)speedSP);

            //posSP.X = posSP.X - 10;
            //UpdateUiSetpointPosition();
        }
    }
}
