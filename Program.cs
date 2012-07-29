using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using PSMoveSharp;
using WindowsInput;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;

namespace MouseTest
{    
    static class Program
    {
        public static PSMoveClientThreadedRead moveClient;
        public static Form1 form;
        public static Thread updateGuiThread;
        public static bool _updateGuiThreadQuit;

        public static bool client_connected = false, client_paused = false;
        public static uint update_delay = 4;// in milliseconds

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Get the ball rolling on update delegate
            _updateGuiThreadQuit = false;
            updateGuiThread = new Thread(new ThreadStart(updateGui));
            
            // Form setup
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();

            // Start update thread and application
            updateGuiThread.Start();
            Application.Run( form );

            // After run, stop thread and disconnect client
            _updateGuiThreadQuit = true;
            updateGuiThread.Join();

            client_disconnect();
        }

        // Calls the update delegate to receive new coords from the Move
        private static void updateGui()
        {
            while (_updateGuiThreadQuit == false)
            {
                try
                {
                    form.Invoke(form.updateGuiDelegate);
                    Thread.Sleep(Convert.ToInt32(update_delay));
                }
                catch
                {
                    return;
                }
            }
        }

        // Connects to the client (the PS3)
        public static void client_connect(String server_address, int server_port)
        {
            moveClient = new PSMoveClientThreadedRead();

            try
            {
                moveClient.Connect(Dns.GetHostAddresses(server_address)[0].ToString(), server_port);
                moveClient.StartThread();
            }
            catch
            {
                return;
            }

            client_connected = true;

            Properties.Settings.Default.most_recent_server = server_address;
            Properties.Settings.Default.Save();
        }

        // Disconnect from PS3
        public static void client_disconnect()
        {
            try
            {
                client_paused = false;

                moveClient.StopThread();
                moveClient.Close();
            }
            catch
            {
                return;
            }

            client_connected = false;
        }
    }
}
