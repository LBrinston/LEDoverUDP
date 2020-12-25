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
 * @brief: GUI controller for WS2812b addressable LED. Control is done via UDP packets sent to an ESP32 which is wired to the LEDs.
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
        LEDMath LEDmath = new LEDMath();
        public UdpClient udpClient = new UdpClient();
        public SolidColorBrush solidColorBrush = new SolidColorBrush(); // Used for displaying the currently selected colour
        public IPAddress targetIP; // Holds the user inputted IPAddress for use with IPEndPoint object


        //public IPAddress (byte[] address);
        public byte[] address = new byte[4]; // Holds the quartet found in the dotted quad representation of IP inputted by the user.
       
        public int targetPort; // Holds port for UDP connection to target
        public bool fadeMode = false;
        public bool selectorMode = true; // Selector mode is default

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

        /// <summary>
        /// Tries to establish a UDP connection with user input'd IPAddress and Port #
        /// https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient.connect?view=net-5.0
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
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


        /// <summary>
        /// Updates the IPAddress var when the textbox loses keyboard focus
        /// https://docs.microsoft.com/en-us/dotnet/api/system.net.ipaddress.parse?view=net-5.0
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtIPAddr_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // IPAdress.Parse is a static method. It is called on the class not the IPAddress instance
            targetIP = IPAddress.Parse(txtIPAddr.Text);

            // Should update the displayed current port configuration
            txtIPAddrCur.Text = targetIP.ToString();
        }

        /// <summary>
        /// Updates the targetPort variable after user input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtPort_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            targetPort = Int32.Parse(txtPort.Text);
            txtPortCur.Text = targetPort.ToString();
        }


        /// <summary>
        /// Updates the checksum when the datagram values have been changed by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtDatagram_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            updateChkSum();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            Byte[] sendBytes; // Used in selectorMode
            if (selectorMode == true)
            {
                sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n"); // Concatenate up our sendBytes
                try // Try to send our UDP packet
                {
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                catch (Exception f) // Catch if the user forget to connect before trying to send
                {
                    txtErrors.Text = "Did you remember to establish a UDP connection? \n";
                    txtErrors.Text += f.ToString(); // If there is an error push it to the Error reporting box
                    
                }
            }
            
            else if (fadeMode == true)
            {

                LEDmath.calcColourDiffs();

                //Debug to the error box
                //txtErrors.Text = "####Before thread#####";
                //txtErrors.Text += "\n ColourFrom00:" + fromLED0;
                //txtErrors.Text += "\n ColourTo00:" + toLED0;
                //txtErrors.Text += "\n ColourDelta00:" + colourDelta00;
                //txtErrors.Text += "\n ColourStep00:" + colourStep00;
                //txtErrors.Text += "\n ####Before thread end#####";

                // START FADE
                // Start by setting the From colours on each LED before invoking helper thread
                editDatagramColour(LEDmath.fromLED0, 5, 6);
                editDatagramColour(LEDmath.fromLED1, 13, 6);
                updateChkSum();
                sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n");
                try
                {
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                catch (Exception f) // Catch if the user forget to connect before trying to send
                {
                    txtErrors.Text = "Did you remember to establish a UDP connection? \n";
                    txtErrors.Text = f.ToString(); // If there is an error push it to the Error reporting box
                }

                //Multi-threading
                var thread = new Thread(new ThreadStart(fadeThread));
                // Now activate the helper thread to handle the rest of the fade
                thread.Start();
            }

        }

        /// <summary>
        /// Callback function utilized to carry out fading between colours. Uses a Timespan interval to Thread.sleep(interval) the thread after interrations through the for loop  
        /// </summary>
        private void fadeThread()
        {
            TimeSpan interval = new TimeSpan(0,0,0,0,50); // 100ms "delay" interval
            
            for (int i = 0; i < LEDmath.fadeStep; i++) // Loop # of times specified in LEDMath.fadeStep to achieve colour fade
            {
                // https://stackoverflow.com/questions/9732709/the-calling-thread-cannot-access-this-object-because-a-different-thread-owns-it
                // https://docs.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher.invoke?view=net-5.0
                // From the docs: "only the thread that created a DispatcherObject may access that object. For example, a background thread that is spun off from 
                // the main UI thread cannot update the contents of a Button that was created on the UI thread. In order for the background thread to access the Content property 
                // of the Button, the background thread must delegate the work to the Dispatcher associated with the UI thread.
                this.Dispatcher.Invoke((Action)(() => // Lambda magic for granting thead access to UI elements? 
                {
                    //Debug to the error box
                    //txtErrors.Text += "\n ------Helper Thread
                    //txtErrors.Text += "\n ColourFrom00:" + fromLED0;
                    //txtErrors.Text += "\n ColourTo00:" + toLED0;
                    //txtErrors.Text += "\n ColourDelta00:" + colourDelta00;
                    //txtErrors.Text += "\n ColourStep00:" + colourStep00;
                    //txtErrors.Text += "\n colourInter00:" + colourIntermediate00; // print to our error box

                    LEDmath.fadeColour(); // Updates intermediate colour values
                    editDatagramColour(LEDmath.colourIntermediate00, 5, 6); // Use those intermediate colours to edit the datagram
                    editDatagramColour(LEDmath.colourIntermediate01, 13, 6);
                    txtErrors.Text = packetBuilder.checkPacket(txtDatagram.Text); // Double check our datagram is the correct length and report to the Error box if not
                    updateChkSum();
                    Byte[] sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n");
                    
                    try
                    {
                        udpClient.Send(sendBytes, sendBytes.Length); // Send the UDP packet
                    }
                    catch (Exception f) // Catch if the user forget to connect before trying to send
                    {
                        txtErrors.Text = f.ToString(); // If there is an error push it to the Error reporting box
                    }
                }));
                Thread.Sleep(interval); //"Delay" thread before resuming loop
            }

            // Now that we've finished our incrementing up to our "To colour" we must be sure we've made it 
            // all they way by sending one last packet with the "To colour"
            this.Dispatcher.Invoke((Action)(() => 
            {
                // Final step is to be sure we make it to the "To" colour
                editDatagramColour(LEDmath.toLED0, 5, 6);
                editDatagramColour(LEDmath.toLED1, 13, 6);
                txtErrors.Text = packetBuilder.checkPacket(txtDatagram.Text); 
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

        /// <summary>
        /// Called by the slider scroll event handler to update colour values for LEDMath class and the displayed values on the slider
        /// </summary>
        public void updateColourValues()
        {
            byte bRed = Convert.ToByte(scrollBarRed.Value); // Must be a byte because alpha channels are specified in hex
            byte bGreen = Convert.ToByte(scrollBarGreen.Value);
            byte bBlue = Convert.ToByte(scrollBarBlue.Value);

            uint colourCode = ((uint)bRed << 16) + ((uint)bGreen << 8) + (uint)bBlue; // Create a single colour value from our byte colour values

            // Update the channel values displayed next to the sliders
            txtRed.Text = bRed.ToString();
            txtGreen.Text = bGreen.ToString();
            txtBlue.Text = bBlue.ToString();

            // Check what mode we're in 
            if (selectorMode == true)
            { 
                // Check which LED we're adjusting the colour channels for 
                if (rbtnLED0.IsChecked == true)
                {
                    editDatagramColour(colourCode, 5, 6); // Update the colour values in the datagram
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
                    // Check the datagram length!
                    txtErrors.Text = packetBuilder.checkPacket(txtDatagram.Text);
                    btnColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue)); // Update the colour preview
                    btnColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                }
            }
            
            if (fadeMode == true)
            {
                // Check what the colour slider should be applied to 
                if (rbtnFromLED0.IsChecked == true)
                {
                    LEDmath.fromLED0 = colourCode;
                    LEDmath.colourIntermediate00 = colourCode; // This must initially equal our fromColour
                    btnFromColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    LEDmath.fromLED0R = bRed; // For fade it will be necessary to capture the individual colour channels as well
                    LEDmath.fromLED0G = bGreen;
                    LEDmath.fromLED0B = bBlue;
                }
                else if (rbtnToLED0.IsChecked == true)
                {
                    LEDmath.toLED0 = colourCode;
                    btnToColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    LEDmath.toLED0R = bRed;
                    LEDmath.toLED0G = bGreen;
                    LEDmath.toLED0B = bBlue;

                }
                else if (rbtnFromLED1.IsChecked == true)
                {
                    LEDmath.fromLED1 = colourCode;
                    LEDmath.colourIntermediate01 = colourCode;
                    btnFromColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    LEDmath.fromLED1R = bRed;
                    LEDmath.fromLED1G = bGreen;
                    LEDmath.fromLED1B = bBlue;

                }
                else if (rbtnToLED1.IsChecked == true)
                {
                    LEDmath.toLED1 = colourCode;
                    btnToColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
                    LEDmath.toLED1R = bRed;
                    LEDmath.toLED1G = bGreen;
                    LEDmath.toLED1B = bBlue;

                }
            }

            updateChkSum();
        }

        /// <summary>
        /// Removes and inserts the 6 Byte Hex colour value in the Datagram at the specified Index, Length
        /// </summary>
        /// <param name="colourCode"></param>
        /// <param name="editIndex"></param>
        /// <param name="editLength"></param>
        public void editDatagramColour(uint colourCode, int editIndex, int editLength)
        {
            String colourValue = colourCode.ToString("X6");
            txtDatagram.Text = txtDatagram.Text.Remove(editIndex, editLength);
            txtDatagram.Text = txtDatagram.Text.Insert(editIndex, colourValue);
        }

        /// <summary>
        /// Updates the checksum value displayed on the GUI
        /// </summary>
        public void updateChkSum()
        {
            //String CheckSum = Convert.ToString(packetBuilder.calChkSum(txtDatagram.Text), 10
            String CheckSum = (packetBuilder.calChkSum(txtDatagram.Text)).ToString("D3"); // For some reason this still drops leadig zeros

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
 
        /// <summary>
        /// Calls to updateColourValues when the scrollbar has been scrolled 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
        updateColourValues();
        }

        /// <summary>
        /// Moves scollbars back to the colour values specified in the datagram for the LED that has been selected via radiobutton
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnLED0_Checked(object sender, RoutedEventArgs e)
        {
            scrollBarRed.Value = Convert.ToInt32(txtDatagram.Text.Substring(5, 2), 16);
            txtRed.Text = Convert.ToString(scrollBarRed.Value);
      
            scrollBarGreen.Value = Convert.ToInt32(txtDatagram.Text.Substring(7, 2), 16);
            txtGreen.Text = Convert.ToString(scrollBarGreen.Value);

            scrollBarBlue.Value = Convert.ToInt32(txtDatagram.Text.Substring(9, 2), 16);
            txtBlue.Text = Convert.ToString(scrollBarBlue.Value);
        }

        /// <summary>
        /// Moves scollbars back to the colour values specified in the datagram for the LED that has been selected via radiobutton
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Hides the UDP configuration and error reporting when checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkConfig_Checked(object sender, RoutedEventArgs e)
        {
            UDPStack.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Makes visible when the UDP configuration and error reporting when unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkConfig_Unchecked(object sender, RoutedEventArgs e)
        {
            UDPStack.Visibility = Visibility.Visible;
        }

        /// <summary>
        ///  Triggers UI element swap when selectMode button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Triggers UI element swap when fadeMode button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFadeMode_Click(object sender, RoutedEventArgs e)
        {
            // Swap UI elements
            indivStack.Visibility = Visibility.Collapsed;
            fadeStack.Visibility = Visibility.Visible;
            
            // Toggle modes
            fadeMode = true;
            selectorMode = false;

            btnSend.Content = "Faaade";
            comboModeSelector.IsDropDownOpen = false; // Close the combobox after a selection has been made
        }

        /// <summary>
        /// Empties the string in the Error reporting box on click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearErrors_Click(object sender, RoutedEventArgs e)
        {
            txtErrors.Text = ""; // Empty the error reporting box if the clear button is clicked
        }
    }
}
    

