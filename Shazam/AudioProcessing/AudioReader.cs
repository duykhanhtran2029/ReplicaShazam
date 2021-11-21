using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using NAudio.Wave;
using Shazam.AudioFormats;
using Shazam.Tools;
using Complex = System.Numerics.Complex;

namespace Shazam.AudioProcessing.Server
{
	public class AudioReader
	{
		/// <summary>
		/// Creates AudioFormat from an audio file
		/// </summary>
		/// <param name="path">Location of the audio file</param>
		/// <returns>Audio format with parsed information</returns>
		public static IAudioFormat GetSound(string path)
		{
			//TODO: Wav converter for more type
			if (path.EndsWith(".mp3"))
            {
				path = WavConverter(path);
            }

			byte[] data = File.ReadAllBytes(path);
			IAudioFormat Sound;

			if (path.EndsWith(".wav"))
			{
				Sound = new WavFormat();
				//Check if the beginning starts with RIFF (currently only supported format of wav files)
				if (!Sound.IsCorrectFormat(new []{ data[0], data[1], data[2], data[3] }))
				{
					throw new ArgumentException($"File {path} formatted wrongly, not a 'wav' format.");
				}

				Sound.Channels = Converter.BytesToUInt(new byte[] {data[22], data[23] });
				Sound.SampleRate = Converter.BytesToUInt(new byte[] {data[24], data[25], data[26] , data[27] });
				Sound.ByteRate = Converter.BytesToInt(new byte[] {data[28], data[29], data[30] , data[31] });
				Sound.BlockAlign = Converter.BytesToShort(new byte[] {data[32], data[33] });
				Sound.BitsPerSample = Converter.BytesToShort(new byte[] {data[34], data[35] });

				//gathering actual sound data
				int dataOffset = FindDataOffset(data);
				//nubmer of bytes divide by two (short = 2 bytes && 1 sample = 1 short)
				Sound.NumOfDataSamples = Converter.BytesToInt(new byte[]
					{data[dataOffset - 4], data[dataOffset - 3], data[dataOffset - 2], data[dataOffset - 1]}) / 2; 
				var byteData = data.Skip(dataOffset).Take(Sound.NumOfDataSamples*2).ToArray();
				Sound.Data = GetSoundDataFromBytes(byteData);
			}
			else
			{
				throw new NotImplementedException($"Format of file {path} is not implemented.");
			}

			return Sound;
		}

		/// <summary>
		/// Finds offset of audio data in metadata 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static int FindDataOffset(byte[] data)
		{
			for (int i = 0; i < data.Length - 3; i++)
			{
				if (data[i] == 0x64 && //d
				    data[i + 1] == 0x61 && //a
				    data[i + 2] == 0x74 && //t
				    data[i + 3] == 0x61)//a
				{
					return i + 8; //+4 is for 'data' bytes, +4 is for integer representing number of data bytes
				}
			}

			throw new ArgumentException("Part with data not found.");
		}

		/// <summary>
		/// Transforms byte audio data into short audio data
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static short[] GetSoundDataFromBytes(byte[] data)
		{
			short[] dataShorts = new short[data.Length / 2];

			for (int i = 0; i < data.Length; i += 2)
			{
				dataShorts[i / 2] = Converter.BytesToShort(new byte[] { data[i], data[i + 1] });
			}

			return dataShorts;
		}

		/// <summary>
		/// Transforms mp3 to wav
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string WavConverter(string path)
		{
			int outRate = 48000;
			int bitsPerSample = 16;
			int channel = 1;

			//redirect to MP3 Folder
			var reader = new Mp3FileReader(path.Replace("Songs/Wav/", "Songs/Mp3/"));

			//converting
			var outFormat = new WaveFormat(outRate, bitsPerSample, channel);
			var resampler = new MediaFoundationResampler(reader, outFormat);
			path = path.Replace(".mp3", ".wav");
			WaveFileWriter.CreateWaveFile($"{path}", resampler);

			return path;
		}
	}
}
