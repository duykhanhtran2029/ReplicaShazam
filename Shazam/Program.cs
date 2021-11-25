using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using NAudio.Wave;
using Shazam.AudioFormats;
using Shazam.AudioProcessing;
using Shazam.AudioProcessing.Server;

namespace Shazam
{
    class Program
	{

        public static void Main()
		{
            
            DatabaseConnection.CreateConnection();

            Shazam s = new Shazam();
            
            //WavConvert("ZW6F9A6U");
            //AudioMixer.NoiseMixer("ZWZF8Z96");
            //CutRawSong("ZWZF8Z96");
            //LoadAllSongs(s);
            //MixAllSongs(s);
            //TestAllSongs(s);
            Menu(s);


        }

        private static void Menu(Shazam s)
        {
            TextWriter output = Console.Out;

            Thread command = null;

            output.WriteLine("{1,2} {0}", "Enter 'h' or 'help' for help.", "");
            while (true)
            {
                string argument = Console.ReadLine();
                switch (argument.ToLower())
                {
                    case "a":
                    case "add":
                        output.WriteLine("{1,2} {0}", "Enter name of the audio file", "");
                        string path = Console.ReadLine();
                        if (command != null && command.IsAlive)
                            command.Join();
                        command = new Thread(() =>
                        {
                            AddSong(path, s);
                        });
                        command.Start();

                        break;
                    case "c":
                    case "clear":
                        if (output == Console.Out)
                            Console.Clear();
                        break;

                    case "h":
                    case "help":
                        WriteHelp(output);
                        break;

                    case "l":
                    case "list":
                        s.ListSongs(output);
                        break;

                    case "r":
                    case "record":
                        if (command != null && command.IsAlive)
                            command.Join();
                        command = new Thread(() =>
                        {
                            output.WriteLine("{0,2} {1}", "", s.RecognizeSong());
                        });
                        command.Start();
                        break;

                    case "g":
                    case "get":
                        output.WriteLine("{1,2} {0}", "Enter name of the audio file", "");
                        string fpath = Console.ReadLine();
                        if (command != null && command.IsAlive)
                            command.Join();
                        command = new Thread(() =>
                        {
                            output.WriteLine("{0,2} {1}", "", s.RecognizeFile(fpath));
                        });
                        command.Start();
                        break;

                    default:
                        continue;

                }

            }
        }

        private static void AddSong(string path, Shazam s)
        {
			try
			{
				s.AddNewSong(path);
			}
			catch (Exception e)
			{
				if (e is FileNotFoundException)
				{
					Console.WriteLine(e.Message);
                    Console.WriteLine("File not found.");
				}
				else if (e is ArgumentException)
				{
                    Console.WriteLine(e.Message);
                    Console.WriteLine("{0,2} {1}", "", $"Adding was unsuccessful.");
				}
				else throw e;

			}
		}
        private static void WriteHelp(TextWriter o)
        {
	        Tuple<string, string>[] commandList = 
	        {
				new Tuple<string, string>("a | add", "add new song to the database"),
				new Tuple<string, string>("c | clear", "clear the console"),
				new Tuple<string, string>("h | help", "list all commands"),
				new Tuple<string, string>("l | list", "list all songs in the database"),
				new Tuple<string, string>("r | record", "record an audio and recognize the song"),
                new Tuple<string, string>("g | get", "get record from file and recognize the song")
            };
	        o.WriteLine("{2,2} {0,-10} {1,-20}\n", "COMMAND", "DESCRIPTION","");

			foreach (var pair in commandList)
	        {
		        o.WriteLine("{2,2} {0,-10} {1,-20}", pair.Item1, pair.Item2, "");
	        }
        } 

        private static void LoadAllSongs(Shazam s, string folder = "Mp3")
        {
            DirectoryInfo d = new DirectoryInfo($"Resources/Songs/{folder}");
            // Note: If it was mp3 file ? nOt Converted?????
            string searchPattern = folder == "Mp3" ? "*.mp3" : "*.wav";
            FileInfo[] Files = d.GetFiles(searchPattern);
            foreach (FileInfo file in Files)
            {
                AddSong(file.Name, s);
            }
        }
        private static void CutRawSong(string name)
        {
            string input = $"Resources/Songs/Wav/{name}.wav";

            string output = $"Resources/Songs/Cut/{name}.wav";
            using (AudioFileReader reader = new AudioFileReader(input))
            {
                TimeSpan startPosition = TimeSpan.Parse("00:01:00.000");
                TimeSpan endPosition = TimeSpan.Parse("00:01:10.000");

                IWaveProvider cut = AudioMixer.AudioCutter(reader, startPosition, endPosition);

                var outFormat = new WaveFormat(48000, 16, 1);
                var resampler = new MediaFoundationResampler(cut, outFormat);

                WaveFileWriter.CreateWaveFile(output, resampler);
            }
        }
        private static void WavConvert(string name)
        {
            string input = $"Resources/Songs/Wav/{name}.mp3";
            AudioReader.WavConverter(input);
        }
        private static void MixAllSongs(Shazam s)
        {
            DirectoryInfo d = new DirectoryInfo($"Resources/Songs/Wav");
            FileInfo[] Files = d.GetFiles("*.wav");
            foreach (FileInfo file in Files)
            {
                AudioMixer.NoiseMixer(file.Name.Replace(".wav",""));
            }
        }
        private  static void TestAllSongs(Shazam s)
        {
            DirectoryInfo d = new DirectoryInfo($"Resources/Songs/Wav");
            FileInfo[] Files = d.GetFiles("*.wav");
            List<Tuple<string, bool, string, string>> testResults = new List<Tuple<string, bool, string, string>>();
            foreach (FileInfo file in Files)
            {
                string rs = s.RecognizeFile(file.Name, true);
                string[] result = rs.Split("-");

                if (result[0] == file.Name)
                {
                    testResults.Add(new Tuple<string, bool, string, string>(file.Name.Replace(".wav", ""), true, result[1], result[2]));
                    Console.WriteLine($"{file.Name} : true in {result[2]}");
                }
                else
                {
                    testResults.Add(new Tuple<string, bool, string, string>(file.Name.Replace(".wav", ""), false, "NaN", "NaN"));
                    Console.WriteLine($"{file.Name} : false in {result[2]}");
                }
            }

            string json = JsonSerializer.Serialize(testResults);
            json = json.Replace("Item1", "Name").Replace("Item2","Result").Replace("Item3","Score").Replace("Item4","Time");
            
            File.WriteAllText(@"Resources/TestResult.json", json);
            Console.WriteLine("All songs are tested");
        }
    }	
}
