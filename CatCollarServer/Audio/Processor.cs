﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CatCollarServer.Audio
{
    public class Processor
    {
        public WavData WaveData { get; private set; }
        public List<Frame> Frames { get; private set; }
        private Dictionary<uint, Tuple<uint, uint>> frameToRaw;

        private uint samplesPerFrame;

        public List<Word> Words { get; private set; }
        private Dictionary<uint, Tuple<uint, uint>> wordToFrames;
        public double RMSMax { get; private set; }
        public double WordsThreshold { get; private set; }

        public Processor(WavData wavData)
        {
            WaveData = wavData;
            Frames = new List<Frame>();
            frameToRaw = new Dictionary<uint, Tuple<uint, uint>>();
            samplesPerFrame = 0;
            Words = new List<Word>();
            wordToFrames = new Dictionary<uint, Tuple<uint, uint>>();
            RMSMax = 0;
            WordsThreshold = 0;
        }

        public Word GetAsWholeWord()
        {
            UseAllSamplesAsOneWord();
            return Words.FirstOrDefault();
        }

        public void Init()
        {
            // Init "samples per frame" measure
            uint bytesPerFrame = (uint)(WaveData.Header.BytesPerSec * AudioParameters.FRAME_LENGTH / 1000.0);
            uint bytesPerSample = (uint)(WaveData.Header.BitsPerSample / 8);
            samplesPerFrame = (uint)(bytesPerFrame / bytesPerSample);
            if (samplesPerFrame <= 0)
            {
                throw new InvalidOperationException("There are no samples per frame");
            }

            //Main part of splitting
            DivideIntoFrames();
        }

        private void DivideIntoFrames()
        {
            WavData wavData = (WavData)WaveData.Clone();

            uint samplesPerNonOverlap = (uint)(samplesPerFrame * (1 - AudioParameters.FRAME_OVERLAP));
            uint framesCount = (uint)(WaveData.Header.Subchunk2Size / (wavData.Header.BitsPerSample / 8));
            uint begin = 0, end = 0;

            for (uint frameId = 0, size = wavData.NumberOfSamples; frameId < framesCount; frameId++)
            {
                begin = frameId * samplesPerNonOverlap;
                end = begin + samplesPerFrame;
                if (end < size)
                {
                    Frame frame = new Frame(frameId);
                    frame.Init(WaveData.RawData, WaveData.NormalizedData, begin, end);
                    //Frames.Insert((int)(Frames.IndexOf(Frames.FirstOrDefault()) + frameId), frame);
                    Frames.Insert((int)frameId, frame);
                    frameToRaw.Add(frameId, new Tuple<uint, uint>(begin, end));
                }
                else
                {
                    break;
                }
            }
        }

        public void DivideIntoWords()
        {
            if (Frames.Count <= 10)
            {
                throw new InvalidOperationException("Not enaught frames");
            }

            // Find silence threshold
            bool hasSilence = FindSilenceThreshold();

            // Divide frames into words
            int wordId = -1;
            long firstFrameInCurrentWordNumber = -1;
            Word lastWord = null;

            if (hasSilence)
            {
                List<Frame> frames = Frames.Select(x => (Frame)x.Clone()).ToList();
                foreach (Frame frame in frames)
                {
                    //Got a sound
                    if (frame.RMS > WordsThreshold)
                    {
                        if (firstFrameInCurrentWordNumber == -1)
                        {
                            firstFrameInCurrentWordNumber = frame.Id;
                            Console.WriteLine("Word started at frame {0}", (int)firstFrameInCurrentWordNumber);
                        }
                    }
                    //Got a silence
                    else
                    {
                        if (firstFrameInCurrentWordNumber >= 0)
                        {
                            //Find distance between start of the current word and end of the previous word
                            ProcessSilence(frame, ref lastWord, ref firstFrameInCurrentWordNumber, ref wordId);
                        }
                    }
                }

                // Clean up short words
                CleanUpWords();
            }
            // There is no any silence in the sound
            else
            {
                UseAllSamplesAsOneWord();
            }

            // If has only one word let's consider whole sample
            if (hasSilence && Words.Count == 1)
            {
                UseAllSamplesAsOneWord();
            }
        }

        private uint ProcessSilence(Frame frame, ref Word lastWord, ref long firstFrameInCurrentWordNumber, ref int wordId)
        {
            uint distance = 0;
            if (lastWord != null)
            {
                uint lastFrameInPreviousWordNumber = wordToFrames[lastWord.Id].Item2;
                distance = (uint)(firstFrameInCurrentWordNumber - lastFrameInPreviousWordNumber);
            }

            //Has a new word
            if (lastWord == null || distance >= AudioParameters.WORDS_MIN_DISTANCE)
            {
                wordId++;
                lastWord = new Word((uint)wordId);

                wordToFrames.Add(lastWord.Id, new Tuple<uint, uint>((uint)firstFrameInCurrentWordNumber, frame.Id));
                Words.Add(lastWord);

                Console.WriteLine("We have a word {0} ({1} - {2})", lastWord.Id, firstFrameInCurrentWordNumber, frame.Id);
            }
            // Need to add the current word to the previous one
            else if (lastWord != null && distance < AudioParameters.WORDS_MIN_DISTANCE)
            {
                // Compute RMS for current word
                double currentWordRms = 0;
                for (int i = (int)firstFrameInCurrentWordNumber; i < frame.Id; i++)
                {
                    currentWordRms += Frames[i].RMS;
                }
                currentWordRms /= frame.Id - firstFrameInCurrentWordNumber;

                // Add the word only if it has valuable RMS
                if (currentWordRms > WordsThreshold * 2)
                {
                    uint firstFrameInPreviousWordNumber = wordToFrames[lastWord.Id].Item1;
                    wordToFrames.Remove(lastWord.Id);
                    wordToFrames.Add(lastWord.Id, new Tuple<uint, uint>(firstFrameInPreviousWordNumber, frame.Id));

                    Console.WriteLine("Word {0} will be extended ({1} - {2})", lastWord.Id, wordToFrames[lastWord.Id].Item1, frame.Id);
                }
            }
            firstFrameInCurrentWordNumber = -1;
            return distance;
        }

        //Clean up short words
        private void CleanUpWords()
        {
            //Word word;
            for (uint i = Words.FirstOrDefault().Id; i < Words.Count;)
            {
                //word = Words[(int)i];
                if (GetFramesCount(Words[(int)i]) < AudioParameters.WORD_MIN_SIZE)
                {
                    Console.WriteLine("Word {0} is too short and will be avoided", i);
                    wordToFrames.Remove(i);
                    /*word = Words[(int)i + 1];*/
                    Words[(int)i] = Words[(int)i + 1];
                }
                else
                {
                    i++;
                }
            }
        }
        private void UseAllSamplesAsOneWord()
        {
            Words.Clear();
            wordToFrames.Clear();

            Word oneWord = new Word(0);

            wordToFrames.Add(oneWord.Id, new Tuple<uint, uint>(Frames.First().Id, Frames.Last().Id));
            Words.Add(oneWord);
            Console.WriteLine("Seems has only one word in the sample... All frames will be added into the word!");
        }
        private bool FindSilenceThreshold()
        {
            // Find max and min rms/entropy
            double rms, rmsMax, rmsSilence = 0;
            rms = Frames.FirstOrDefault().RMS;
            rmsMax = rms;

            // Try to guess the best threshold value
            bool hasSilence = false;
            uint count = 0;
            foreach (Frame frame in Frames)
            {
                rms = frame.RMS;
                rmsMax = Math.Max(rmsMax, rms);
                if (frame.Entropy < AudioParameters.ENTROPY_THRESHOLD)
                {
                    hasSilence = true;
                    rmsSilence += frame.RMS;
                    count++;
                }
            }
            rmsSilence /= count;

            RMSMax = rmsMax;
            WordsThreshold = rmsSilence * 2;

            return hasSilence;
        }

        public void InitMFCC(ref Word word)
        {
            uint firstId = wordToFrames[word.Id].Item1;
            uint lastId = wordToFrames[word.Id].Item2;

            uint framesCount = lastId - firstId + 1;
            double[] mfcc = new double[AudioParameters.MFCC_SIZE * framesCount];

            for (uint i = 0; i < framesCount; i++)
            {
                uint rawBegin = frameToRaw[firstId + i].Item1;
                uint rawEnd = frameToRaw[firstId + i].Item2;

                double[] frameMFCC = Frames[(int)firstId + 1].InitMFCC(WaveData.NormalizedData, rawBegin, rawEnd, WaveData.Header.SamplesPerSec);

                for (uint j = 0; j < AudioParameters.MFCC_SIZE; j++)
                {
                    mfcc[i * AudioParameters.MFCC_SIZE + 1] = frameMFCC[j];
                }
            }
            word.SetMFCC(mfcc, AudioParameters.MFCC_SIZE * framesCount);
        }

        public void SaveWordAsAudio(in string filePathName, Word word)
        {
            // Number of data bytes in the resulting wave file
            uint samplesPerNonOverlap = (uint)(samplesPerFrame * (1 - AudioParameters.FRAME_OVERLAP));
            uint waveSize = GetFramesCount(word) * samplesPerNonOverlap * sizeof(short);

            string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, filePathName);

            // Prepare a new header and write it to file stream
            WavHeader header = new WavHeader();
            WaveData.Header.RIFF.CopyTo(0, header.RIFF.ToArray(), 0, 4);
            header.ChunkSize = waveSize + WavData.WavHeaderSizeOf;
            WaveData.Header.Wave.CopyTo(0, header.Wave.ToArray(), 0, 4);
            WaveData.Header.FMT.CopyTo(0, header.FMT.ToArray(), 0, 4);
            header.Subchunk1Size = WaveData.Header.Subchunk1Size;
            header.AudioFormat = WaveData.Header.AudioFormat;
            header.NumberOfChan = 1;
            header.SamplesPerSec = WaveData.Header.SamplesPerSec;
            header.BytesPerSec = WaveData.Header.SamplesPerSec * sizeof(short);
            header.BlockAlign = sizeof(short);
            header.BitsPerSample = sizeof(short) * 8;
            WaveData.Header.Data.CopyTo(0, header.Data.ToArray(), 0, 4);
            header.Subchunk2Size = waveSize;

            BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Open));

            short[] data = new short[waveSize / sizeof(short)];

            int frameNumber = 0;

            int frameStart = -1;
            for (uint currentFrame = wordToFrames[word.Id].Item1; currentFrame < wordToFrames[word.Id].Item2; currentFrame++)
            {
                frameStart = (int)frameToRaw[currentFrame].Item1;
                for (uint i = 0; i < samplesPerNonOverlap; i++)
                {
                    data[frameNumber * samplesPerNonOverlap + i] = WaveData.RawData[frameStart + i];
                }
                frameNumber++;
            }
            writer.Write(Serialization.Serialize(data), 0, (int)waveSize);
            writer.Close();
        }

        public bool IsPartOfWord(Frame frame)
        {
            bool isPartOfWord = false;

            for(uint i = 0; i < Words.Count; i++)
            {
                if(wordToFrames[i].Item1 <= frame.Id && frame.Id <= wordToFrames[i].Item2)
                {
                    isPartOfWord = true;
                    break;
                }
            }
            return isPartOfWord;
        }

        public uint GetFramesCount(Word word)
        {
            return wordToFrames[word.Id].Item2 - wordToFrames[word.Id].Item1;
        }
    }
}
