using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace jvccameraclient
{

    public partial class Form1 : Form
    {

        byte[] buffer = new byte[300000]; //1080 frame is 209374
        int read, total = 0;
        private bool streaming1 = false;
        private bool streaming2 = false;
        private bool streaming3 = false;
        private bool streaming4 = false;
        public bool isMQTTregistered = false;
        UdpClient alarmReceiver = new UdpClient(7023); // using port 7023 to receive UDP packets from the cameras
        bool alarmReceiverRunning = false;


        public Form1()
        {
            InitializeComponent();
            textBox1.AppendText(DateTime.Now + ": " + "Startup... \r\n");
        }

        //stream camera 2
        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(DateTime.Now + ": " + "Attempting to get a single frame from camera 2... \r\n");
            //IPAddress[] addresses = Dns.GetHostAddresses(Convert.ToString(textBox3.Text)); // the user may put in a web address or a hostnmae, so let's account for that. 
            string server = textBox3.Text;
            var endpoint = new Uri("http://" + server + "/api/video?encode=jpeg(1)&framerate=0&server_push=on ");
            // tells the camera to start streaming using the 1st profile/config. Example: use framerate 0 to get just one frame... use 5 to get 5 frames per second 

            //client.GetAsync(endpoint); // fire off the GET request to the camera and async receive the data.
            //orignal method that worked

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endpoint);
            // get response
            WebResponse resp = req.GetResponse();
            // get response stream
            Stream stream = resp.GetResponseStream();
            // read data from stream
            while ((read = stream.Read(buffer, total, 1515)) != 0)
            {
                total += read;
            }
            // get bitmap
            Bitmap bmp = (Bitmap)Image.FromStream(
                          new MemoryStream(buffer, 59, total));
            Bitmap img2 = Resize(bmp);
            label4.Visible = false;
            pictureBox2.Image = img2;

            stream.Flush();
            total = 0;
            read = 0;
        }

        //camera 1
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(DateTime.Now + ": " + "Attempting to get a single frame from camera 1... \r\n");
            string server = textBox2.Text;
            var endpoint = new Uri("http://" + server + "/api/video?encode=jpeg(1)&framerate=0&server_push=on ");
            // tells the camera to start streaming using the 1st profile/config. Example: use framerate 0 to get just one frame... use 5 to get 5 frames per second 

            //client.GetAsync(endpoint); // fire off the GET request to the camera and async receive the data.
            //orignal method that worked

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endpoint);
            // get response
            WebResponse resp = req.GetResponse();
            // get response stream
            Stream stream = resp.GetResponseStream();
            // read data from stream
            while ((read = stream.Read(buffer, total, 1515)) != 0)
            {
                total += read;
            }
            // get bitmap
            Bitmap bmp = (Bitmap)Image.FromStream(
                          new MemoryStream(buffer, 59, total));
            Bitmap img2 = Resize(bmp);
            label3.Visible = false;
            pictureBox1.Image = img2;

            stream.Flush();
            total = 0;
            read = 0;
        }

        private void button3_Click(object sender, EventArgs e) // button that tells starts the streaming from camera 2
        {
            // tells the camera to start streaming using the 1st profile/config. Example: use framerate 0 to get just one frame... use 5 to get 5 frames per second 

            //client.GetAsync(endpoint); // fire off the GET request to the camera and async receive the data.
            //orignal method that worked

            // get response stream
            streaming2 = true;
            textBox1.AppendText(DateTime.Now + ": " + "Streaming camera 2.\r\n");
            backgroundWorker1.RunWorkerAsync();

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)  // stream from camera 2
        {
            // get response stream

            while (streaming2 == true)
            {
                string server = textBox3.Text;
                var endpoint = new Uri("http://" + server + "/api/video?encode=jpeg(1)&framerate=0&server_push=on ");
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endpoint);
                WebResponse resp = req.GetResponse();

                Stream stream = resp.GetResponseStream();
                // read data from stream
                while ((read = stream.Read(buffer, total, 1515)) != 0)
                {
                    total += read;
                }
                // get bitmap

                Bitmap bmp = (Bitmap)Image.FromStream(
                              new MemoryStream(buffer, 59, total));
                Bitmap img2 = Resize(bmp);

                pictureBox2.Invoke(new Action(() => pictureBox2.Image = img2));


                stream.Flush();
                total = 0;
                read = 0;
            }
        }

        private void button4_Click(object sender, EventArgs e) // stop the stream from camera 2
        {
            streaming2 = false;
            textBox1.AppendText(DateTime.Now + ": " + "Stop streaming camera 2.\r\n");
            backgroundWorker1.CancelAsync();
        }

        private void button5_Click(object sender, EventArgs e) // enable motion detection for camera 2
        {
            textBox1.AppendText(DateTime.Now + ": " + "Enabling motion detection on camera 2.\r\n");
            var client = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.status=on ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }
            var client2 = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint2 = new Uri("http://" + server + "/api/param?camera.detection.status=save ");
                client2.DefaultRequestHeaders.Add("Accept", " text/plain");
                client2.DefaultRequestHeaders.Add("Host", " " + server);
                client2.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client2.GetAsync(endpoint2); // fire off the GET request to the camera
            }

            if (alarmReceiverRunning != true)
            {
                alarmReceiver.BeginReceive(dataReceived, alarmReceiver);
                alarmReceiverRunning = true;
            }
        }

        private void mqttClientRegister()
        {
            textBox1.AppendText(DateTime.Now + ": " + "Regsitering with MQTT Server.\r\n");
            if (textBox6 != null)
            {
                try
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(Convert.ToString(textBox6.Text)); // the user may put in a web address or a hostnmae, so let's account for that. 
                    string server = Convert.ToString(addresses[1]);
                    mqttClient = new MqttClient(server, 1883, false, null, null, MqttSslProtocols.None);
                }
                catch
                {
                    string server = Convert.ToString(textBox6.Text);
                    mqttClient = new MqttClient(server, 1883, false, null, null, MqttSslProtocols.None);
                }

                mqttClient.Connect("JVCCameraClient-" + Guid.NewGuid().ToString());   // now that we have a client and a client ID, lets try to connect to the server. 
                isMQTTregistered = true;  // set a flag that shows that the client has been registered. Shoudl also log this.
                textBox1.AppendText(DateTime.Now + ": " + "MQTT server registation complete.\r\n");
            }
            else
            {
                MessageBox.Show("Please enter an IP address for the MQTT server!");
            }
        }

        private void dataReceived(IAsyncResult ar) // we know that cameras are going to send a message to 7023 when they have motion, find out which one sent the alarm. 
        {
            UdpClient client = (UdpClient)ar.AsyncState;
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Byte[] receivedBytes = client.EndReceive(ar, ref iPEndPoint);
            //convert the receievd data
            string receivedText = ASCIIEncoding.ASCII.GetString(receivedBytes);
            CameraData data = JsonConvert.DeserializeObject<CameraData>(receivedText);
            this.textBox1.Invoke(new Action(() =>              // for troubleshooting purposes, stick the message in a big, multi-line textbox so we can see it. 
            {
                this.textBox1.AppendText(DateTime.Now + ": " + receivedText + "\r\n");       // same as above. 
            }));
            //send MTQQ message
            if (isMQTTregistered == true)
            {
                string topic = textBox7.Text;
                string messageString = "IN" + data.JVCDATA.camera.ToString() + "RaisingEvent";
                mqttClient.Publish(Convert.ToString(topic), Encoding.UTF8.GetBytes(messageString), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);  // uset he publish method from the M2MQTT library
            }// Restart listening for udp data packages
            alarmReceiver.BeginReceive(dataReceived, ar.AsyncState);
        }

        private void button6_Click(object sender, EventArgs e)// set the motion detect area for camera 2
        {
            textBox1.AppendText(DateTime.Now + ": " + "Configuring camera.\r\n");
            // for 16:9
            //string allOFF = "0000000000000000000000000000000000";  // clear the whole area 
            string allON = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";   // set/enable the whole area 
            var client = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.area=" + allON + " ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }
            var client2 = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint2 = new Uri("http://" + server + "/api/param?camera.detection.level=90 ");
                client2.DefaultRequestHeaders.Add("Accept", " text/plain");
                client2.DefaultRequestHeaders.Add("Host", " " + server);
                client2.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client2.GetAsync(endpoint2); // fire off the GET request to the camera
            }
            var client3 = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint3 = new Uri("http://" + server + "/api/param?camera.detection.level.status=save ");
                client3.DefaultRequestHeaders.Add("Accept", " text/plain");
                client3.DefaultRequestHeaders.Add("Host", " " + server);
                client3.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client3.GetAsync(endpoint3); // fire off the GET request to the camera
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // tells the camera to start streaming using the 1st profile/config. Example: use framerate 0 to get just one frame... use 5 to get 5 frames per second 

            //client.GetAsync(endpoint); // fire off the GET request to the camera and async receive the data.
            //orignal method that worked

            // get response stream
            streaming1 = true;
            textBox1.AppendText(DateTime.Now + ": " + "Getting stream from camera 1.\r\n");
            backgroundWorker2.RunWorkerAsync();
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            // get response stream

            while (streaming1 == true)
            {
                string server = textBox2.Text;
                var endpoint = new Uri("http://" + server + "/api/video?encode=jpeg(1)&framerate=0&server_push=on ");
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endpoint);
                WebResponse resp = req.GetResponse();

                Stream stream = resp.GetResponseStream();
                // read data from stream
                while ((read = stream.Read(buffer, total, 1515)) != 0)
                {
                    total += read;
                }
                // get bitmap

                Bitmap bmp = (Bitmap)Image.FromStream(
                              new MemoryStream(buffer, 59, total));
                Bitmap img2 = Resize(bmp);
                pictureBox1.Invoke(new Action(() => pictureBox1.Image = img2));

                stream.Flush();
                total = 0;
                read = 0;
            }
        }

        private void button8_Click(object sender, EventArgs e) // set the motion detect area for camera 2
        {
            textBox1.AppendText(DateTime.Now + ": " + "Enabling motion detection on camera 1.\r\n");
            var client = new HttpClient();
            {
                string server = textBox2.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.status=on ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }

            if (alarmReceiverRunning != true)
            {
                alarmReceiver.BeginReceive(dataReceived, alarmReceiver);
                alarmReceiverRunning = true;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(DateTime.Now + ": " + "Configuring camera 1.\r\n");
            // for 16:9
            //string allOFF = "0000000000000000000000000000000000";  // clear the whole area 
            string allON = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";   // set/enable the whole area 
            var client = new HttpClient();
            {
                string server = textBox2.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.area=" + allON + " ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }
            var client2 = new HttpClient();
            {
                string server = textBox2.Text;
                var endpoint2 = new Uri("http://" + server + "/api/param?camera.detection.level=90 ");
                client2.DefaultRequestHeaders.Add("Accept", " text/plain");
                client2.DefaultRequestHeaders.Add("Host", " " + server);
                client2.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client2.GetAsync(endpoint2); // fire off the GET request to the camera
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            streaming1 = false;
            textBox1.AppendText(DateTime.Now + ": " + "Stopping the stream on camera 1.\r\n");
            backgroundWorker2.CancelAsync();
        }

        private void button21_Click(object sender, EventArgs e) // register the mqtt client
        {
            mqttClientRegister();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(DateTime.Now + ": " + "Saving motion detection on camera 1.\r\n");
            var client = new HttpClient();
            {
                string server = textBox2.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.status=save ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(DateTime.Now + ": " + "Saving motion detect area and sensitivity for camera 1.\r\n");
            var client = new HttpClient();
            {
                string server = textBox2.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.area.status.save ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }
            var client2 = new HttpClient();
            {
                string server = textBox2.Text;
                var endpoint2 = new Uri("http://" + server + "/api/param?camera.detection.level.status=save ");
                client2.DefaultRequestHeaders.Add("Accept", " text/plain");
                client2.DefaultRequestHeaders.Add("Host", " " + server);
                client2.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client2.GetAsync(endpoint2); // fire off the GET request to the camera
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(DateTime.Now + ": " + "Saving motion detection on camera 2.\r\n");
            var client = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.status=save ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(DateTime.Now + ": " + "Saving motion detect area and sensitivity for camera 2.\r\n");
            var client = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint = new Uri("http://" + server + "/api/param?camera.detection.area.status.save ");
                client.DefaultRequestHeaders.Add("Accept", " text/plain");
                client.DefaultRequestHeaders.Add("Host", " " + server);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client.GetAsync(endpoint); // fire off the GET request to the camera
            }
            var client2 = new HttpClient();
            {
                string server = textBox3.Text;
                var endpoint2 = new Uri("http://" + server + "/api/param?camera.detection.level.status=save ");
                client2.DefaultRequestHeaders.Add("Accept", " text/plain");
                client2.DefaultRequestHeaders.Add("Host", " " + server);
                client2.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", " Basic YWRtaW46anZj");
                client2.GetAsync(endpoint2); // fire off the GET request to the camera
            }
        }

        private static new Bitmap Resize(Bitmap image)
        {
            // Get the image current width
            int sourceWidth = 1920;
            // Get the image current height
            int sourceHeight = 1080;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            // Calculate width and height with new desired size
            nPercentW = ((float)500 / (float)sourceWidth);
            nPercentH = ((float)500 / (float)sourceHeight);
            nPercent = Math.Min(nPercentW, nPercentH);
            // New Width and Height
            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height
            g.DrawImage(image, 0, 0, destWidth, destHeight);
            g.Dispose();
            return b;

        }
    }
    //{"JVCDATA": {"camera": 2, "alarm": "motion"}}
    public class CameraData   // this is the main  bundle of json formatted data coming from the server
    {
        public Body JVCDATA { get; set; }  // this points to the nested dataset containd in the message:
    }   // "body" is the name of the nested information from the json message 

    public class Body  // this is the nested json data we are looking for
    {
        public string camera { get; set; }
        public string alarm { get; set; }
        //public string status { get; set; }
    }

}
