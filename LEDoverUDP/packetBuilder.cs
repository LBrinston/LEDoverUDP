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


        public int calChkSum(String controlVals)
        {
            ChkSum = 0;

            // Start from 3 because we don't consider the ### in calculating the checksum
            for (int i = 3; i < controlVals.Length; i++)
            {
                ChkSum += (byte)controlVals[i];
            }
            ChkSum %= 1000;
            return ChkSum;
        }

        public void fadeCalcs() { 
        }

    }

}
