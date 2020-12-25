using System;
using System.Collections.Generic;
using System.Text;

namespace LEDoverUDP
{
    class LEDMath
    {
        public int fadeStep = 20; // Default 20ms delay
        public int fadeDelay = 20; // 20ms default fade time

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

        public LEDMath()
        {

        }


        /// <summary>
        /// Calculates a single 26 bit (packaged as a 32bit) colour value from separate RGB colour channels
        /// </summary>
        /// <param name="Red"></param>
        /// <param name="Green"></param>
        /// <param name="Blue"></param>
        /// <returns>A 26bit colour value in a 32bit package</returns>
        public uint getColourCode(uint Red, uint Green, uint Blue)
        {
            uint colourCode = 0;
            return colourCode = (Red << 16) + (Green << 8) + Blue;
        }

        /// <summary>
        /// Increments the Intermediate colour value by the difference calculated by calcColourDiffs.
        /// Must be called in a loop constrained to the # of LEDMath.fadeStep to achieve fade.
        /// </summary>
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


        /// <summary>
        /// Calculates the distance between individual colour channels.
        /// </summary>
        public void calcColourDiffs()
        {
            // Calc deltas - only need to happen once
            diffLED0R = (toLED0R - fromLED0R) / fadeStep;
            diffLED0G = (toLED0G - fromLED0G) / fadeStep;
            diffLED0B = (toLED0B - fromLED0B) / fadeStep;

            diffLED1R = (toLED1R - fromLED1R) / fadeStep;
            diffLED1G = (toLED1G - fromLED1G) / fadeStep;
            diffLED1B = (toLED1B - fromLED1B) / fadeStep;
        }

    }
}
