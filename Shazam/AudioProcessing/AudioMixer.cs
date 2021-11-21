using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shazam.AudioProcessing
{
    public partial class AudioMixer
    {
        /// <summary>
        /// Audio cutter
        /// </summary>
        /// <param name="wave"></param>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        public static IWaveProvider AudioCutter(WaveStream wave,
                                     TimeSpan startPosition,
                                     TimeSpan endPosition)
        {

            ISampleProvider sourceProvider = wave.ToSampleProvider();

            // Take audio from startPosition to endPosition
            OffsetSampleProvider start = new OffsetSampleProvider(sourceProvider)
            {
                SkipOver = startPosition,
                Take = (endPosition - startPosition)
            };

            return (start.ToWaveProvider());

        }

        /// <summary>
        /// Audio mixer
        /// </summary>

        public static void NoiseMixer(string name)
        {
            string inputFile = $"Resources/Songs/Wav/{name}.wav";

            string whiteNoiseFile = "Resources/Songs/Noise/WhiteNoise.wav";
            string rainNoiseFile = "Resources/Songs/Noise/RainNoise.wav";
            string carNoiseFile = "Resources/Songs/Noise/CarNoise.wav";
            string machineNoiseFile = "Resources/Songs/Noise/MachineNoise.wav";
            string textingNoiseFile = "Resources/Songs/Noise/TextingNoise.wav";
            string brownNoiseFile = "Resources/Songs/Noise/BrownNoise.wav";
            string pinkNoiseFile = "Resources/Songs/Noise/PinkNoise.wav";
            string potNoiseFile = "Resources/Songs/Noise/PotNoise.wav";
            string chairNoiseFile = "Resources/Songs/Noise/ChairNoise.wav";

            string outputFile = $"Resources/Songs/Mix/{name}.wav";


            using (AudioFileReader reader = new AudioFileReader(inputFile))
            {
                TimeSpan startPosition = TimeSpan.Parse("00:01:00.000");
                TimeSpan endPosition = TimeSpan.Parse("00:01:10.000");

                IWaveProvider cut = AudioCutter(reader, startPosition, endPosition);
                IWaveProvider whiteNoise = new AudioFileReader(whiteNoiseFile);
                IWaveProvider rainNoise = new AudioFileReader(rainNoiseFile);
                IWaveProvider carNoise = new AudioFileReader(carNoiseFile);
                IWaveProvider machineNoise = new AudioFileReader(machineNoiseFile);
                IWaveProvider textingNoise = new AudioFileReader(textingNoiseFile);
                IWaveProvider brownNoise = new AudioFileReader(brownNoiseFile);
                IWaveProvider pinkNoise = new AudioFileReader(pinkNoiseFile);
                IWaveProvider potNoise = new AudioFileReader(potNoiseFile);
                IWaveProvider chairNoise = new AudioFileReader(chairNoiseFile);

                var mixer = new MixingWaveProvider32(new[] 
                { 
                    cut, 
                    whiteNoise, rainNoise, carNoise, machineNoise, 
                    textingNoise, brownNoise, pinkNoise, potNoise, chairNoise});

                var outFormat = new WaveFormat(48000, 16, 1);
                var resampler = new MediaFoundationResampler(mixer, outFormat);

                WaveFileWriter.CreateWaveFile(outputFile, resampler);
            }

        }
    }
}
