using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Threading;    // Needed for fade mode
using System.Diagnostics; // Stopwatch code used with threading comes from here

/* 
 * 
 * @brief: GUI controller for WS2812b addressable LED strips via UDP packets.
 * @author: Liam Brinston
 * 
 */


namespace LEDoverUDP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        packetBuilder packetBuilder = new packetBuilder();
        UdpClient udpClient = new UdpClient();
        SolidColorBrush solidColorBrush = new SolidColorBrush(); // Used for displaying the currently selected colour

        //public IPAddress (byte[] address);
        public byte[] address = new byte[4]; // Holds the quartet found in the dotted quad representation of IP inputted by the user.
        public IPAddress targetIP; // Holds the user inputted IPAddress for use with IPEndPoint object
        public int targetPort; // Holds port for UDP connection to target
        public bool fadeMode = false;
        public bool selectorMode = true; // Selector mode is default

        // Fade mode variables
        public int colourDelta00; // Colour delta for LED00 - should be signed
        public uint colourStep00;
        
        public int colourDelta01; // Colour delta for LED01 - should be signed
        public uint colourStep01;
        
        public int fadeStep = 20; // Default 20ms delay

        public uint fromLED0;
        public uint toLED0;
        public uint fromLED1;
        public uint toLED1;

        public double fromLED0R;
        public double fromLED0G;
        public double fromLED0B;
        public double toLED0R;
        public double toLED0G;
        public double toLED0B;
               
        public double fromLED1R;
        public double fromLED1G;
        public double fromLED1B;
        public double toLED1R;
        public double toLED1G;
        public double toLED1B;

        public double diffLED0R;
        public double diffLED0G;
        public double diffLED0B;
        public double diffLED1R;
        public double diffLED1G;
        public double diffLED1B;

        public uint colourIntermediate00;
        public uint colourIntermediate01;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }


        // Fires on loading of MainWindow
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            // May or may not use this
            //throw new NotImplementedException();
        }

        // Tries to establish a UDP connection with user input'd IPAddress and Port #
        // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient.connect?view=net-5.0
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Establish UDP connection here?


            IPEndPoint ipEndPoint = new IPEndPoint(targetIP, targetPort);

            try
            {
                udpClient.Connect(ipEndPoint);
            }
            catch (Exception f)
            {
                txtErrors.Text = f.ToString(); // If there is an error push it to the Error reporting box
            }

        }


        // Updates the IPAddress var when the textbox loses keyboard focus
        // https://docs.microsoft.com/en-us/dotnet/api/system.net.ipaddress.parse?view=net-5.0
        private void txtIPAddr_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {

            // IPAdress.Parse is a static method. It is called on the class not the IPAddress instance
            targetIP = IPAddress.Parse(txtIPAddr.Text);

            // Should update the displayed current port configuration
            txtIPAddrCur.Text = targetIP.ToString();
        }

        //Updates the targetPort variable after user input
        private void txtPort_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            targetPort = Int32.Parse(txtPort.Text);
            txtPortCur.Text = targetPort.ToString();
            // Should update the displayed current port configuration
        }

        // Updates the checksum when the datagram values have been changed by the user
        private void txtDatagram_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {

            updateChkSum();

        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            Byte[] sendBytes; // Used in selectorMode
            if (selectorMode == true)
            {
                sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n");

                // Try to send
                try
                {
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                catch (Exception f) // Catch if the user forget to connect before trying to send
                {
                    txtErrors.Text = f.ToString(); // If there is an error push it to the Error reporting box
                }
            }
            // This is going to regarding threading for delay purposes
            else if (fadeMode == true)
            {

                calcColourDeltas();

                //Debug to the error box
                //txtErrors.Text = "####Before thread#####";
                //txtErrors.Text += "\n ColourFrom00:" + fromLED0;
                //txtErrors.Text += "\n ColourTo00:" + toLED0;
                //txtErrors.Text += "\n ColourDelta00:" + colourDelta00;
                //txtErrors.Text += "\n ColourStep00:" + colourStep00;
                //txtErrors.Text += "\n ####Before thread end#####";

                // START FADE
                // Start by setting the From colours on each LED before invoking helper thread
                editDatagramColour(fromLED0, 5, 6);
                editDatagramColour(fromLED1, 13, 6);
                updateChkSum();
                sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n");
                try
                {
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                catch (Exception f) // Catch if the user forget to connect before trying to send
                {
                    txtErrors.Text = f.ToString(); // If there is an error push it to the Error reporting box
                }

                //Multi-threading
                var thread = new Thread(new ThreadStart(ExecuteInForeground));
                // Now activate the helper thread to handle the rest of the fade
                thread.Start();
            }

        }

        // Callback function utilized for the fade function
        private void ExecuteInForeground()
        {
            //Byte[] sendBytes;
            //var stopWatch = Stopwatch.StartNew();
            TimeSpan interval = new TimeSpan(0,0,0,1); // 100ms "delay" interval
            
            for (int i = 0; i < 21; i++)
            {
                //Calcs must occur up here I think? Otherwise they vanish with the lambda call?
                colourIntermediate00 = fromLED0 + colourStep00; // This never seems to change

                // https://stackoverflow.com/questions/9732709/the-calling-thread-cannot-access-this-object-because-a-different-thread-owns-it
                // https://docs.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher.invoke?view=net-5.0
                // From the docs: "only the thread that created a DispatcherObject may access that object. For example, a background thread that is spun off from 
                // the main UI thread cannot update the contents of a Button that was created on the UI thread. In order for the background thread to access the Content property 
                // of the Button, the background thread must delegate the work to the Dispatcher associated with the UI thread.
                this.Dispatcher.Invoke((Action)(() => // Lambda magic for granting thead access to UI elements? 
                {
                    ////Debug to the error box
                    //txtErrors.Text += "\n ------Helper Thread
                    //txtErrors.Text += "\n ColourFrom00:" + fromLED0;
                    //txtErrors.Text += "\n ColourTo00:" + toLED0;
                    //txtErrors.Text += "\n ColourDelta00:" + colourDelta00;
                    //txtErrors.Text += "\n ColourStep00:" + colourStep00;
                    //txtErrors.Text += "\n colourInter00:" + colourIntermediate00; // print to our error box

                    fadeColour(); // Updates intermediate colour values
                    editDatagramColour(colourIntermediate00, 5, 6);
                    editDatagramColour(colourIntermediate01, 13, 6);
                    updateChkSum();
                    Byte[] sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n");
                    
                    //txtErrors.Text += "\n sendBytes:" + (BitConverter.ToString(sendBytes));
                    
                    try
                    {
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                    catch (Exception f) // Catch if the user forget to connect before trying to send
                    {
                        txtErrors.Text = f.ToString(); // If there is an error push it to the Error reporting box
                    }
                }));
                Thread.Sleep(interval); //"Delay" thread before resuming loop
            }

            this.Dispatcher.Invoke((Action)(() => 
            {

                // Final step is to be sure we make it to the "To" colour
                editDatagramColour(toLED0, 5, 6);
                editDatagramColour(toLED1, 13, 6);
                updateChkSum();
                Byte[] sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n");
                try
                {
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                catch (Exception f) // Catch if the user forget to connect before trying to send
                {
                    txtErrors.Text = f.ToString(); // If there is an error push it to the Error reporting box
                }

            }));
            // Fade complete
        }


        public void updateColourValues()
        {
            byte bRed = Convert.ToByte(scrollBarRed.Value); // Must be a byte because alpha channels are specified in hex
            byte bGreen = Convert.ToByte(scrollBarGreen.Value);
            byte bBlue = Convert.ToByte(scrollBarBlue.Value);

            uint colourCode = ((uint)bRed << 16) + ((uint)bGreen << 8) + (uint)bBlue; // Create a single colour value from our byte colour values

            // Update the channel values
            txtRed.Text = bRed.ToString();
            txtGreen.Text = bGreen.ToString();
            txtBlue.Text = bBlue.ToString();



            // Check what mode we're in
            if (selectorMode == true)
            {
                // Update the colour values in the datagram
                // BUT first check which LED we're adjusting the colour channels for 
                if (rbtnLED0.IsChecked == true)
                {
                    editDatagramColour(colourCode, 5, 6);
                    btnColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue)); // Update the colour preview
                }
                else if (rbtnLED1.IsChecked == true)
                {
                    editDatagramColour(colourCode, 13, 6);
                    btnColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                }
                else if (rbtnSync.IsChecked == true)
                {
                    editDatagramColour(colourCode, 5, 6);
                    editDatagramColour(colourCode, 13, 6);
                    btnColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue)); // Update the colour preview
                    btnColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                }
            }
            
            if (fadeMode == true)
            {
                // implement fade mode
                if (rbtnFromLED0.IsChecked == true)
                {
                    fromLED0 = colourCode;
                    colourIntermediate00 = colourCode; // This must initially equal our fromColour
                    btnFromColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    fromLED0R = bRed;
                    fromLED0G = bGreen;
                    fromLED0B = bBlue;
                }
                else if (rbtnToLED0.IsChecked == true)
                {
                    toLED0 = colourCode;
                    btnToColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    toLED0R = bRed;
                    toLED0G = bGreen;
                    toLED0B = bBlue;

                }
                else if (rbtnFromLED1.IsChecked == true)
                {
                    fromLED1 = colourCode;
                    colourIntermediate01 = colourCode;
                    btnFromColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    fromLED1R = bRed;
                    fromLED1G = bGreen;
                    fromLED1B = bBlue;

                }
                else if (rbtnToLED1.IsChecked == true)
                {
                    toLED1 = colourCode;
                    btnToColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    toLED1R = bRed;
                    toLED1G = bGreen;
                    toLED1B = bBlue;

                }
            }

            updateChkSum();
        }

        public void fadeColour()
        {
            // Add the diff to each channel
            fromLED0R += diffLED0R; 
            fromLED0G += diffLED0G;
            fromLED0B += diffLED0B;

            fromLED1R += diffLED1R;
            fromLED1G += diffLED1G;
            fromLED1B += diffLED1B;

            // Squash all those channels into one and update our intermediate colour value
            colourIntermediate00 = getColourCode((uint)fromLED0R, (uint)fromLED0G, (uint)fromLED0B);
            colourIntermediate01 = getColourCode((uint)fromLED1R, (uint)fromLED1G, (uint)fromLED1B);
        }

        public void calcColourDeltas ()
        {
            // Calc deltas - only need to happen once
            diffLED0R = (toLED0R - fromLED0R) / fadeStep;
            diffLED0G = (toLED0G - fromLED0G) / fadeStep;
            diffLED0B = (toLED0B - fromLED0B) / fadeStep;

            diffLED1R = (toLED1R - fromLED1R) / fadeStep;
            diffLED1G = (toLED1G - fromLED1G) / fadeStep;
            diffLED1B = (toLED1B - fromLED1B) / fadeStep;
        }

        public uint getColourCode(uint Red, uint Green, uint Blue)
        {
            uint colourCode = 0;
            return colourCode = (Red << 16) + (Green << 8) + Blue;
        }


        // Datagram wrangling

        public void editDatagramColour(uint colourCode, int editIndex, int editLength)
        {
            String colourValue = colourCode.ToString("X6");
            txtDatagram.Text = txtDatagram.Text.Remove(editIndex, editLength);
            txtDatagram.Text = txtDatagram.Text.Insert(editIndex, colourValue);
        }


        public void updateChkSum()
        {
            String CheckSum = Convert.ToString(packetBuilder.calChkSum(txtDatagram.Text), 10);

            // Check if we've dropped any leading zeros on our String representaton of the CheckSum 
            if (CheckSum.Length == 1)
            {
                txtChksum.Text = "00" + CheckSum;
            }
            else if (CheckSum.Length == 2)
            {
                txtChksum.Text = "0" + CheckSum;
            }
            else
            {
                txtChksum.Text = CheckSum;
            }

        }

        // Event Handlers
        private void scrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            updateColourValues();
        }

        // Moves scollbars back to the colour values specified in the datagram
        private void rbtnLED0_Checked(object sender, RoutedEventArgs e)
        {
            scrollBarRed.Value = Convert.ToInt32(txtDatagram.Text.Substring(5, 2), 16);
            txtRed.Text = Convert.ToString(scrollBarRed.Value);
      
            scrollBarGreen.Value = Convert.ToInt32(txtDatagram.Text.Substring(7, 2), 16);
            txtGreen.Text = Convert.ToString(scrollBarGreen.Value);

            scrollBarBlue.Value = Convert.ToInt32(txtDatagram.Text.Substring(9, 2), 16);
            txtBlue.Text = Convert.ToString(scrollBarBlue.Value);
        }

        private void rbtnLED1_Checked(object sender, RoutedEventArgs e)
        {
            scrollBarRed.Value = Convert.ToInt32(txtDatagram.Text.Substring(13, 2), 16);
            txtRed.Text = Convert.ToString(scrollBarRed.Value);
            scrollBarGreen.Value = Convert.ToInt32(txtDatagram.Text.Substring(15, 2), 16);
            txtGreen.Text = Convert.ToString(scrollBarGreen.Value);
            scrollBarBlue.Value = Convert.ToInt32(txtDatagram.Text.Substring(17, 2), 16);
            txtBlue.Text = Convert.ToString(scrollBarBlue.Value);
        }

        private void rbtnSync_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void chkConfig_Checked(object sender, RoutedEventArgs e)
        {
            UDPStack.Visibility = Visibility.Collapsed;
        }

        private void chkConfig_Unchecked(object sender, RoutedEventArgs e)
        {
            UDPStack.Visibility = Visibility.Visible;
        }

        private void btnSelectorMode_Click(object sender, RoutedEventArgs e)
        {
            // Swap UI elements for different modes
            indivStack.Visibility = Visibility.Visible;
            fadeStack.Visibility = Visibility.Collapsed;
            
            //Toggle modes
            selectorMode = true;
            fadeMode = false;
            btnSend.Content = "Send";
            comboModeSelector.IsDropDownOpen = false;
        }

        private void btnFadeMode_Click(object sender, RoutedEventArgs e)
        {
            // Swap UI elements
            indivStack.Visibility = Visibility.Collapsed;
            fadeStack.Visibility = Visibility.Visible;
            
            // Toggle modes
            fadeMode = true;
            selectorMode = false;

            btnSend.Content = "Faaade";
            comboModeSelector.IsDropDownOpen = false;
        }

        private void clearErrors_Click(object sender, RoutedEventArgs e)
        {
            txtErrors.Text = ""; // Empty the error reporting box if the clear button is clicked
        }
    }
}
    

