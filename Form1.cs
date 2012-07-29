using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PSMoveSharp;
using WindowsInput;

namespace MouseTest
{
    public partial class Form1 : Form
    {
        // Setup 
        public int serverPort = 7899;
        public delegate void ProcessPSMoveStateDelegate();
        public ProcessPSMoveStateDelegate updateGuiDelegate;

        static UInt32 processed_packet_index = 0;
        static UInt16 last_buttons = 0;
        
        public Form1()
        {
            InitializeComponent();
            updateGuiDelegate = update;
        }

        // Main connect/disconnect button
        private void button1_Click(object sender, EventArgs e)
        {
            if (!Program.client_connected)
            {
                try
                {
                    Program.client_connect(textBox1.Text, serverPort);
                }
                catch
                { }
            }
            else
            {
                Program.client_disconnect();
            }

            if (Program.client_connected)
            {
                button1.Text = "Diskinect";
            }
            else
            {
                button1.Text = "Kinect";
            }
        }

        // Main update function called by the delegate to read in new data from the Move
        // and call various functions based on it.
        private void update()
        {
            Int32 velX, velY, velZ;

            // Return if client isn't connected
            if (Program.moveClient == null)
            {
                return;
            }

            // Get latest data from Move
            PSMoveSharpState state = Program.moveClient.GetLatestState();            
            PSMoveSharpGemState selected_gem = state.gemStates[0];

            if (processed_packet_index == state.packet_index)
            {
                return;
            }

            processed_packet_index = state.packet_index;

            // Process raw input
            velX = Convert.ToInt32((180.0 / Math.PI) * selected_gem.angvel.x);
            velY = Convert.ToInt32((180.0 / Math.PI) * selected_gem.angvel.y);
            velZ = Convert.ToInt32((180.0 / Math.PI) * selected_gem.angvel.z);

            // Pass new data to update both screen and buttons.  updateScreen is called only if updateButtons returns
            // true so screen (mouse) movement can be disabled if a reset button is pressed.
            if ( updateButtons(state) )
                updateScreen(velY, velZ); 
                       
        }

        // Processes keyboard/mouse input based on buttons states on the Move
        private bool updateButtons(PSMoveSharpState state)
        {
            UInt16 just_pressed;
            UInt16 just_released;

            {
                UInt16 changed_buttons = Convert.ToUInt16(state.gemStates[0].pad.digitalbuttons ^ last_buttons);
                just_pressed = Convert.ToUInt16(changed_buttons & state.gemStates[0].pad.digitalbuttons);
                just_released = Convert.ToUInt16(changed_buttons & ~state.gemStates[0].pad.digitalbuttons);
                last_buttons = state.gemStates[0].pad.digitalbuttons;
            }

            // Show the last button being pressed
            textBox2.Text = last_buttons.ToString();

            // Inputs sorted by button state

            // Just pressed

            if (just_pressed == 4)
                return false;

            if (just_pressed == 16)
                InputFunctions.sim_click("right");

            if (just_pressed == 32)
                InputFunctions.keyDown(DirectXKeyCodes.DIK_S);

            if (just_pressed == 64)
                InputFunctions.keyDown(DirectXKeyCodes.DIK_W);

            if(just_released == 128)
                InputFunctions.keyDown(DirectXKeyCodes.DIK_R);

            // Just released

            if (just_released == 32)
                InputFunctions.keyUp((ushort)DirectXKeyCodes.DIK_S);

            if(just_released == 64)
                InputFunctions.keyUp((ushort)DirectXKeyCodes.DIK_W);

            if (just_released == 128)
                InputFunctions.keyUp((ushort)DirectXKeyCodes.DIK_R);

            // Last buttons

            if (last_buttons == 2)
                InputFunctions.sim_click();

            if (last_buttons == 4)
                return false;


            return true;
        }

        // Send mouse movements based on Move-ments.
        private void updateScreen(Int32 X, Int32 Y)
        {
            InputFunctions.sim_mov(-X / 2, -Y / 2);
        }

        // Make sure client disconnects on form close (should be closed already anyway)
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Program.client_disconnect();
        }        
    }
}
