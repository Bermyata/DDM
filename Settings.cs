using System;
using System.IO;

namespace DrawingDataManager
{
    public static class Settings
    {
        public static readonly string FileName = "DrawingData.xlsx";
        public static readonly string FileBlockName = "Blocks.txt";

        public static string[] Names;
        public static void InitializeNames(string filePath)
        {
            Names = File.ReadAllLines(filePath);
        }
    }
}
