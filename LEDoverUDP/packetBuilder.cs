using System;
using System.Collections.Generic;
using System.Text;

namespace LEDoverUDP
{
    class packetBuilder
    { 
        public int ChkSum;

        // Constructor
        public packetBuilder()
        {

        }

        /// <summary>
        /// Calculates the checksum for a packet by summing the decimal ASCII for each character in the string
        /// and %1000 to keep it to 3 Bytes
        /// </summary>
        /// <param name="controlVals"></param>
        /// <returns>int ChkSum</returns>
        public int calChkSum(String packet)
        {
            ChkSum = 0;

            // Start from 3 because we don't consider the ### in calculating the checksum
            for (int i = 3; i < packet.Length; i++)
            {
                ChkSum += (byte)packet[i];
            }
            ChkSum %= 1000;
            return ChkSum;
        }

        /// <summary>
        /// Due to the slapdash nature of the firmware development on the ESP32 end of this project. Packets cannot exceed 23 bytes including the checksum
        /// This double checks the datagram length (23-3 = 19)
        /// </summary>
        /// <param name="datagram"></param>
        /// <returns>String reportBack</returns>
        public String checkPacket(String datagram)
        {
            String reportBack;

            if (datagram.Length == 19)
            {
                return reportBack = ""; // No problem, nothing to report
            }
            else if (datagram.Length > 19)
            {
                return reportBack = "Datagram too long:" + datagram.Length;
            }
            else if (datagram.Length < 19)
            {
                return reportBack = "Datagram too long:" + datagram.Length;
            }
            return reportBack = null; // Should never get here
        }

    }

}
