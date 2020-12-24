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
            Byte[] sendBytes = Encoding.ASCII.GetBytes(txtDatagram.Text + txtChksum.Text + "\r\n");

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

        // Changed to scroll event for debugging
        private void scrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void scrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            updateColourValues();
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

            // Update the colour values in the datagram
            // BUT first check which LED we're adjusting the colour channels for 
            if (rbtnLED0.IsChecked == true)
            {
                // Loosing leading zeros again
                //String colourValue = Convert.ToString(bRed, 16) + Convert.ToString(bGreen, 16) + Convert.ToString(bBlue, 16);
                String colourValue = colourCode.ToString("X6");
                txtDatagram.Text = txtDatagram.Text.Remove(5, 6);
                txtDatagram.Text = txtDatagram.Text.Insert(5, colourValue);
                btnColourLED0.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue)); // Update the colour preview
            }
            else if (rbtnLED1.IsChecked == true)
            {
                String colourValue = colourCode.ToString("X6");
                txtDatagram.Text = txtDatagram.Text.Remove(13, 6);
                txtDatagram.Text = txtDatagram.Text.Insert(13, colourValue);
                btnColourLED1.Background = new SolidColorBrush(Color.FromArgb(255, bRed, bGreen, bBlue));
            }
            updateChkSum();
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

    }
}
    

