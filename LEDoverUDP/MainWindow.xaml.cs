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

namespace LEDoverUDP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {



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

            UdpClient udpClient = new UdpClient();
            IPEndPoint ipEndPoint = new IPEndPoint(targetIP, targetPort);
                        
            try
            {
                udpClient.Connect(ipEndPoint);
            }
            catch (Exception e)
            {
                txtErrors.Text = e.ToString(); // If there is an error push it to the Error reporting box
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
    }
}   
    

