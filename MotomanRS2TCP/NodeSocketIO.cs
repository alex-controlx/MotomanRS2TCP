using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MotomanRS2TCP
{
    class NodeSocketIO
    {

        private bool isConnected = false;
        private bool isConnecting = false;
        private System.Timers.Timer aTimer;
        private MotomanConnection xrc;
        private SocketIO client;
        private bool isDisconnectRequested = false;

        public NodeSocketIO() {}

        public void SetXrc(MotomanConnection xrc)
        {
            this.xrc = xrc;

            xrc.StatusChanged += new System.EventHandler(
                (object sender, EventArgs e) => { if (xrc != null) SendStatus(xrc.GetCopyOfRobotStatus()); }
            );
            xrc.DispatchCurrentPosition += new System.EventHandler(
                (object sender, EventArgs e) => { if (xrc != null) SendPosition(xrc.GetCurrentPositionCached()); }
            );
            xrc.ConnectionError += new System.EventHandler(
                (object sender, EventArgs e) => { if (xrc != null) EmitWrapperError(xrc.CurrentError); }
            );
            //xrc.EventStatus += new System.EventHandler(
            //    (object sender, EventArgs e) => { if (xrc != null) EmitWrapperStatus(xrc.CurrentEvent); }
            //);
            xrc.ConnectionStatus += new System.EventHandler(
                (object sender, EventArgs e) => { if (xrc != null) EmitWrapperStatus(xrc.CurrentConnection); }
            );
            
            
        }

        public void DisposeXrc()
        {
            this.xrc = null;
        }

        public async void Connect()
        {
            if (isConnected || isConnecting) return;
            Console.WriteLine("Connecting to server");
            isConnecting = true;

            // 192.168.1.106:4002 ; 192.168.1.63:4002
            client = new SocketIO("http://192.168.1.106:4002");

            client.OnClosed += Client_OnClosed;
            client.OnConnected += Client_OnConnected;

            // Listen server events
            client.On("toRobot-moveToLoadingHome", async res =>
            {

                if (xrc == null)
                {
                    EmitWrapperError("XRC is undefined");
                    return;
                }

                await xrc.MoveToHome1();
                //Console.WriteLine(res.Text);
                // Next, you might parse the data in this way.
                //var obj = JsonConvert.DeserializeObject<T>(res.Text);
                // Or, read some fields
                //var jobj = JObject.Parse(res.Text);
                //int code = jobj.Value<int>("code");
                //bool hasMore = jobj["data"].Value<bool>("hasMore");
                //var data = jobj["data"].ToObject<ResponseData>();
                // ...
            });

            client.On("toRobot-moveToPosition", async req =>
            {
                Console.WriteLine(req.Text);
                try
                {
                    var jobj = JObject.Parse(req.Text);
                    string respId = jobj.Value<string>("responseChannel");

                    double speed = jobj["data"].Value<double>("speed");
                    IMove iMove = jobj["data"]["iMove"].ToObject<IMove>();

                    if (xrc == null)
                    {
                        await client.EmitAsync(respId, "XRC is undefined");
                        return;
                    }

                    try
                    {
                        //double[] pos = iMove.ToArray();
                        //Console.WriteLine("Moving with speed " + speed + " to " + pos[0] + ", " + pos[1] + ", " + pos[2] + ", " + pos[3] + ", " + pos[4] + ", " + pos[5]);
                        await xrc.MoveIncrementally(iMove, speed);
                        await client.EmitAsync(respId);
                    }
                    catch (Exception ex)
                    {
                        await client.EmitAsync(respId, ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    EmitWrapperError("Parse ERROR: " + ex.Message);
                }


                //var jobj = JObject.Parse(res.Text);
                //double speed = jobj.Value<double>("speed");
                //try
                //{
                    

                //    //double[] pos = iMove.ToArray();
                //    //Console.WriteLine("Moving with speed " + speed + " to " + pos[0] + ", " + pos[1] + ", " + pos[2] + ", " + pos[3] + ", " + pos[4] + ", " + pos[5]);
                //    await xrc.MoveIncrementally(iMove, speed);
                //}
                //catch (Exception ex)
                //{
                //    EmitWrapperError("Parse ERROR: " + ex.Message);
                //}
            });

            client.On("toRobot-cancelAll", async req =>
            {
                Console.WriteLine(req.Text);
                try
                {
                    var jobj = JObject.Parse(req.Text);
                    string respId = jobj.Value<string>("responseChannel");

                    if (xrc == null)
                    {
                        await client.EmitAsync(respId, "XRC is undefined");
                        return;
                    }

                    try
                    {
                        await xrc.CancelOperation();

                        await client.EmitAsync(respId);
                    }
                    catch (Exception ex)
                    {
                        await client.EmitAsync(respId, ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    EmitWrapperError("Parse ERROR: " + ex.Message);
                }
            });

            client.On("toRobot-startJob", async req =>
            {
                Console.WriteLine(req.Text);
                try
                {
                    var jobj = JObject.Parse(req.Text);
                    string respId = jobj.Value<string>("responseChannel");
                    string jobName = jobj.Value<string>("data");

                    if (xrc == null)
                    {
                        await client.EmitAsync(respId, "XRC is undefined");
                        return;
                    }

                    try
                    {
                        await xrc.StartJob(jobName);

                        await client.EmitAsync(respId);
                    }
                    catch (Exception ex)
                    {
                        await client.EmitAsync(respId, ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    EmitWrapperError("Parse ERROR: " + ex.Message);
                }
            });

            client.OnConnected += async () =>
            {
                // Emit identification eevent to the server
                await client.EmitAsync("i-am-robot");
                await Task.Delay(200);
                if (xrc != null) client.EmitAsync("fromRobot-status", xrc.GetCopyOfRobotStatus());
                //// Emit test event, send object.
                //await client.EmitAsync("test", new { code = 200 });
            };

            // Connect to the server
            try {
                await client.ConnectAsync();
            } catch (AggregateException e)
            {
                //Console.WriteLine(e.Message);
                Client_OnClosed(ServerCloseReason.ClosedByServer);
            }
            
        }

        public async Task Disconnect()
        {
            await client.CloseAsync();
            isDisconnectRequested = true;
        }


        private void Client_OnConnected()
        {
            isConnected = true;
            isDisconnectRequested = false;
            Console.WriteLine("Connected to server");
        }

        private void Client_OnClosed(ServerCloseReason serverCloseReason)
        {
            Console.WriteLine("Disconnected from server, " + serverCloseReason.ToString());
            isConnecting = false;
            isConnected = false;
            if (!isDisconnectRequested && serverCloseReason == ServerCloseReason.ClosedByServer) SetTimer();
        }

        private void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += new ElapsedEventHandler((object sender, ElapsedEventArgs e) => Connect());
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }




        private void SendStatus(CRobotStatus status)
        {
            if (!isConnected || client == null ) return;

            client.EmitAsync("fromRobot-status", status);

        }

        private void SendPosition(double[] currentPosition)
        {
            if (!isConnected || client == null) return;

            client.EmitAsync("fromRobot-position", currentPosition);

        }

        private void EmitWrapperError(string message)
        {
            if (!isConnected || client == null) return;

            client.EmitAsync("fromRobot-wrapperError", message);
        }

        private void EmitWrapperStatus(string message)
        {
            if (!isConnected || client == null) return;

            client.EmitAsync("fromRobot-wrapperStatus", message);
        }
    }
}
