using LibLpad;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lpad
{
    internal class WizardMode
    {
        private static readonly string MESSAGE_LOGO = $@"Lms Predictive - Adaptive Differential pulse code modulation (LPAD)
Encoder/Decoder command-line tool.
Copyright (c) 2023 koobar.

Encoder version: {CodecInformation.EncoderVersion}
Decoder version: {CodecInformation.DecoderVersion}
Format version : {CodecInformation.FormatVersion}   <- Encoder/Decoder supports only this version.

================================================================================
If you need any help, Please select operation number '4' and type '-h'.";
        private const string MESSAGE_SELECT_MODE = @"Please select an operation number.
(1) Encode
(2) Decode
(3) Play LPAD encoded file.
(4) Input command-line option.
(5) Exit";
        private const string MES_INPUT_FILE_PATH = "Please input source file full path.";
        private const string MES_ASL_BITS_PER_SAMPLE = "Enter the bits per sample as an integer between 2 and 9.\n" +
            "Specify 0 to use the \"variable\" mode, which dynamically selects the number of quantization bits per block.";
        private const string MES_ASK_BLOCK_SIZE = "Enter the block size as an integer.";
        private const string MES_PROPMT = ">>";

        /// <summary>
        /// ウィザードモードを開始する。
        /// </summary>
        public static void Start()
        {
            Console.Clear();
            Console.WriteLine(MESSAGE_LOGO);
            Console.WriteLine();

            Start:
            Console.WriteLine(MESSAGE_SELECT_MODE);
            int mode = InputNumber("Please enter the operation number.");

            switch (mode)
            {
                case 1:
                    Encode();
                    break;
                case 2:
                    Decode();
                    break;
                case 3:
                    Play();
                    break;
                case 4:
                    CommandLine();
                    break;
                case 5:
                    Environment.Exit(0);
                    break;
            }

            goto Start;
        }

        /// <summary>
        /// エンコード
        /// </summary>
        private static void Encode()
        {
            Console.WriteLine(MES_INPUT_FILE_PATH);
            Console.Write(MES_PROPMT);
            string fileName = Program.ReadFilePath();
            string output = Path.ChangeExtension(fileName, ".lpad");

            // 1サンプルあたりのビット数を尋ねる。
            Console.WriteLine(MES_ASL_BITS_PER_SAMPLE);
            Console.Write(MES_PROPMT);
            int bitsPerSample = 4;
            if (int.TryParse(Console.ReadLine(), out int b))
            {
                bitsPerSample = b;
            }
            Console.WriteLine();

            // ブロックサイズを尋ねる
            Console.WriteLine(MES_ASK_BLOCK_SIZE);
            Console.Write(MES_PROPMT);
            int blockSize = 64;
            if (int.TryParse(Console.ReadLine(), out int bs))
            {
                blockSize = bs;
            }
            Console.WriteLine();
            
            // エンコード
            Program.Encode(fileName, output, bitsPerSample, blockSize);
        }

        /// <summary>
        /// デコード
        /// </summary>
        private static void Decode()
        {
            Console.WriteLine(MES_INPUT_FILE_PATH);
            Console.Write(">>");
            string fileName = Program.ReadFilePath();
            string output = $"{Path.GetDirectoryName(fileName)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(fileName)}_decoded.wav";

            // デコード
            Program.Decode(fileName, output, false);
        }

        /// <summary>
        /// 再生
        /// </summary>
        private static void Play()
        {
            Console.WriteLine(MES_INPUT_FILE_PATH);
            Console.Write(">>");
            string fileName = Program.ReadFilePath();

            // 再生
            Program.PlayFile(fileName);
        }

        /// <summary>
        /// コマンドライン引数を入力して実行
        /// </summary>
        private static void CommandLine()
        {
            Console.WriteLine("Please input a command-line options.");
            Console.Write(">>");

            string cmd = Console.ReadLine() + " -nologo";
            string[] cmds = parseCommandLine();

            CommandLineMode.Start(cmds);
            Console.WriteLine();

            // コマンドライン引数をパースする。
            string[] parseCommandLine()
            {
                List<string> commands = new List<string>();
                bool inLiteral = false;
                string buffer = null;

                foreach(char word in cmd)
                {
                    switch (word)
                    {
                        case '\"':
                            inLiteral = !inLiteral;
                            break;
                        case ' ':
                        case '\t':
                        case '　':
                            if (inLiteral)
                            {
                                goto default;
                            }

                            if(string.IsNullOrEmpty(buffer) == false)
                            {
                                commands.Add(buffer);
                                buffer = null;
                            }
                            break;
                        default:
                            buffer += word;
                            break;
                    }
                }

                if(string.IsNullOrEmpty(buffer) == false)
                {
                    commands.Add(buffer);
                }

                return commands.ToArray();
            }
        }

        /// <summary>
        /// 選択肢番号の入力を求める。
        /// </summary>
        /// <param name="mes"></param>
        /// <returns></returns>
        private static int InputNumber(string mes)
        {
            Console.WriteLine();
            Console.Write(">>");

            string input = Console.ReadLine();

            if(int.TryParse(input, out int i))
            {
                return i;
            }
            else
            {
                var col = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Please enter an integer.");
                Console.ForegroundColor = col;

                return InputNumber(mes);
            }
        }
    }
}
