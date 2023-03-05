using System;
using System.IO;

namespace Lpad
{
    internal class CommandLineMode
    {
        private const string MESSAGE_LOGO = @"Lms Predictive - Adaptive Differential pulse code modulation (LPAD)
Encoder/Decoder command-line tool.
Copyright (c) 2023 koobar.";
        private const string MESSAGE_HELP = "[Command-line options]\n" +
            "-enc(-e)\t:\tEncode mode. Convert WAV (linear pcm) audio file to LPAD.\n" +
            "-bn(n = 2...9)\t:\tSet bits per sample. (ex: if you use -b3 option, encoder use 3 bits per sample.)\n" +
            "-bs\t:\tSet block size. (ex: if you use -bs=1024 option, use blocks of length 1024 samples.)\n" +
            "-dec(-d)\t:\tDecode mode. Convert LPAD format to WAV (16-bit linear pcm).\n" +
            "-play(-p)\t:\tPlay mode. Play output audio file after decode.\n" +
            "-help(-h)\t:\tHelp mode. Show command-line option descriptions. (now mode)\n" +
            "-nologo\t\t:\tIf you use this option, Don't show application logo.";

        /// <summary>
        /// コマンドラインモードを開始する。
        /// </summary>
        /// <param name="args"></param>
        public static void Start(params string[] args)
        {
            LoadOptions(
                args,
                out bool isEncodeMode,
                out bool isDecodeMode,
                out bool isPlayMode,
                out bool isHelpMode,
                out int bitsPerResidual,
                out int blockSize,
                out bool noLogo,
                out string srcFilePath,
                out string destFilePath);

            if (!noLogo)
            {
                Console.WriteLine(MESSAGE_LOGO);
            }

            if (isHelpMode)
            {
                Console.WriteLine(MESSAGE_HELP);
            }
            else if (isEncodeMode)
            {
                Program.Encode(srcFilePath, destFilePath, bitsPerResidual, blockSize);
            }
            else if (isDecodeMode)
            {
                Program.Decode(srcFilePath, destFilePath, isPlayMode);

                if (isPlayMode)
                {
                    Program.PlayFile(srcFilePath);
                }
            }
            else if (isPlayMode)
            {
                Program.PlayFile(srcFilePath);
            }
        }

        /// <summary>
        /// コマンドライン引数から設定を読み込む。
        /// </summary>
        /// <param name="args"></param>
        /// <param name="isEncodeMode"></param>
        /// <param name="isDecodeMode"></param>
        /// <param name="isPlayMode"></param>
        /// <param name="isHelpMode"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="blockSize"></param>
        /// <param name="noLogo"></param>
        /// <param name="srcFilePath"></param>
        /// <param name="destFilePath"></param>
        private static void LoadOptions(
            string[] args,
            out bool isEncodeMode,
            out bool isDecodeMode,
            out bool isPlayMode,
            out bool isHelpMode,
            out int bitsPerSample,
            out int blockSize,
            out bool noLogo,
            out string srcFilePath,
            out string destFilePath)
        {
            isEncodeMode = false;
            isDecodeMode = false;
            isPlayMode = false;
            isHelpMode = false;
            bitsPerSample = 4;
            blockSize = 64;
            noLogo = false;
            srcFilePath = null;
            destFilePath = null;

            foreach(string argument in args)
            {
                if (argument.StartsWith("-"))
                {
                    string[] tmp = argument.Split('=');
                    string argName = tmp[0].ToLower();

                    switch (argName)
                    {
                        case "-out":
                        case "-o":
                            destFilePath = tmp[1];
                            break;
                        case "-help":
                        case "-h":
                            isHelpMode = true;
                            isEncodeMode = false;
                            isDecodeMode = false;
                            isPlayMode = false;
                            break;
                        case "-encode":
                        case "-enc":
                        case "-e":
                            isHelpMode = false;
                            isEncodeMode = true;
                            isDecodeMode = false;
                            isPlayMode = false;
                            break;
                        case "-decode":
                        case "-dec":
                        case "-d":
                            isHelpMode = false;
                            isEncodeMode = false;
                            isDecodeMode = true;
                            isPlayMode = false;
                            break;
                        case "-b2":
                            bitsPerSample = 2;
                            break;
                        case "-b3":
                            bitsPerSample = 3;
                            break;
                        case "-b4":
                            bitsPerSample = 4;
                            break;
                        case "-b5":
                            bitsPerSample = 5;
                            break;
                        case "-b6":
                            bitsPerSample = 6;
                            break;
                        case "-b7":
                            bitsPerSample = 7;
                            break;
                        case "-b8":
                            bitsPerSample = 8;
                            break;
                        case "-b9":
                            bitsPerSample = 9;
                            break;
                        case "-blocksize":
                        case "-bs":
                            blockSize = int.Parse(tmp[1]);
                            break;
                        case "-play":
                        case "-p":
                            isPlayMode = true;
                            break;
                        case "-nologo":
                            noLogo = true;
                            break;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(srcFilePath))
                    {
                        srcFilePath = argument;
                        destFilePath = Path.ChangeExtension(srcFilePath, ".lpad");
                    }
                    else
                    {
                        destFilePath = argument;
                    }
                }
            }
        }
    }
}
