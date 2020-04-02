using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotomanRS2TCP
{
    public partial class Form1 : Form
    {
        private delegate void SafeCallDelegate(string text);
        private NodeTcpListener server;
        private CYasnac xrc;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            StartApp();
        }


        private void Form1_FormClosing(object sender, EventArgs e)
        {
            if (xrc != null)
            {
                xrc.AutoStatusUpdate = false;
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
                xrc = new CYasnac("");
                xrc.StatusChanged += new EventHandler(rc1_StatusChanged); // Register Eventhandler for status change
                xrc.ConnectionStatus += new EventHandler(rc1_connectionStatus); // Register Eventhandler for status change
                xrc.EventStatus += new EventHandler(rc1_eventStatus); // Register Eventhandler for status change
                xrc.ConnectionError += new EventHandler(rc1_errorStatus); // Register Eventhandler for status change
                WriteLine("XRC Starting connection");
                xrc.Connect();
                //rc1.AutoStatusUpdate = true;

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
            WriteLine("    XRC Event: " + xrc.CurrentEvent);
        }

        private void rc1_errorStatus(object sender, EventArgs e)
        {
            WriteLine("    XRC Error: " + xrc.CurrentError);
        }

        private void rc1_StatusChanged(object sender, EventArgs e)
        {
            string message = "IsStep " + xrc.IsStep.ToString() + ", " +
                "1Cycle " + xrc.Is1Cycle.ToString() + ", " +
                "Auto " + xrc.IsAuto.ToString() + ", " +
                "Operating " + xrc.IsOperating.ToString() + ", " +
                "SafeSpeed " + xrc.IsSafeSpeed.ToString() + ", " +
                "Teach " + xrc.IsTeach.ToString() + ", " +
                "Play " + xrc.IsPlay.ToString() + ", " +
                "Teach " + xrc.IsTeach.ToString() + ", " +
                "CommandRemote " + xrc.IsCommandRemote.ToString() + ", " +

                "PlaybackBoxHold " + xrc.IsPlaybackBoxHold.ToString() + ", " +
                "PPHold " + xrc.IsPPHold.ToString() + ", " +
                "ExternalHold " + xrc.IsExternalHold.ToString() + ", " +
                "CommandHold " + xrc.IsCommandHold.ToString() + ", " +
                "Alarm " + xrc.IsAlarm.ToString() + ", " +
                "Error " + xrc.IsError.ToString() + ", " +
                "ServoOn " + xrc.IsServoOn.ToString();
            WriteLine("    XRC Ststus: " + message);
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

        private void btnDown_Click(object sender, EventArgs e)
        {
            WriteLine("Down clicked");

        }

        private void btnGetStatus_Click(object sender, EventArgs e)
        {
            xrc.RefreshStatus();
        }

        private void btnGetPos_Click(object sender, EventArgs e)
        {
            try
            {
                //CRobPosVar posVar = new CRobPosVar();
                double[] p = new double[12];
                xrc.ReadPosition(0, p);  // 27 is Master tool coordinate for XRC and MRC
                //if (posVar.DataType == PosVarType.XYZ)
                //    WriteLine("    X-Value: " + posVar.X.ToString() + "\t Y-Value: " + posVar.Y.ToString() + "\t Z-Value: " + posVar.Z.ToString());
                //else
                //    WriteLine("    S-Value:\t" + posVar.SAxis.ToString());
                WriteLine("    X-Value: " + p[0].ToString() + "\t Y-Value: " + p[1].ToString() + "\t Z-Value: " + p[2].ToString());
                
                double[] doubles = new double[10];
                xrc.ReadByteVariable(0, doubles);
                WriteLine("    Byte var: " + doubles[0].ToString());
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }
    }
}
