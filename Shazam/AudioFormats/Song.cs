using System;
using System.Collections.Generic;
using System.Text;

namespace Shazam.AudioFormats
{
	public class Song
	{
		/// <summary>
		/// Instance of a song
		/// </summary>
		/// <param name="id">song ID</param>
		/// <param name="name">Name of the song</param>
		public Song(uint id, string name)
		{
			ID = id;
			Name = name;
		}

		public uint ID { get; }
		public string Name { get; }

	}
}
