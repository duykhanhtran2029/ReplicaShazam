using System;
using System.Collections.Generic;
using System.Text;

namespace Shazam.AudioFormats
{
    public class TimeFrequencyPoint
    {
		/// <summary>
		/// Instance of a TFPs
		/// </summary>

		public TimeFrequencyPoint(uint time, uint frequency)
		{
			Time = time;
			Frequency = frequency;
		}

		public uint Time { get; }
		public uint Frequency { get; }
	}
}
