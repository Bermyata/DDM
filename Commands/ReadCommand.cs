using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using DrawingDataManager.Commands;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace DrawingDataManager
{
    public class ReadCommand
    {
        [CommandMethod("DDM_export", CommandFlags.Session)]
        public static void Read()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            DocumentCollection acDocMgr = Application.DocumentManager;

            List<Drawing> drawings = new();

            var filePath = acDocMgr.MdiActiveDocument.Name;

            var currentDirectory = Path.GetDirectoryName(filePath);

            Logger logger = new Logger(currentDirectory);

            List<string> documents = Directory.EnumerateFiles(currentDirectory, "*.dwg").ToList();

            acDocMgr.MdiActiveDocument.CloseAndDiscard();

            var hasErrors = false;
            var blockNamesFile = currentDirectory + Path.DirectorySeparatorChar + Settings.FileBlockName;
            if (!File.Exists(blockNamesFile))
            {
                Application.ShowAlertDialog($"{Settings.FileBlockName} is not found in directory: {currentDirectory}");
                logger.Warn($"{Settings.FileBlockName} is not found in directory: {currentDirectory}");
                return;
            }

            Settings.InitializeNames(blockNamesFile);

            foreach (var fileName in documents)
            {
                using (Database db = new Database(false, false))
                {
                    try
                    {
                        db.ReadDwgFile(fileName, FileShare.Read, true, string.Empty);

                        var drawingBlocks = ReadService.ReadBlocks(db, Settings.Names, logger);

                        if (drawingBlocks.Any())
                            drawings.Add(new Drawing(Path.GetFileName(fileName), drawingBlocks));
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error during reading Drawing: {Path.GetFileName(fileName)}{Environment.NewLine}{e.Message}");
                        hasErrors = true;
                    }
                }
            }

            hasErrors = ExcelWriter.WriteData(currentDirectory, drawings, logger) || hasErrors;

            sw.Stop();

            if (hasErrors)
            {
                string directory = AppDomain.CurrentDomain.BaseDirectory;
                Application.ShowAlertDialog($"Errors occured. Check log file in the directory: {directory}");
            }
            else { Application.ShowAlertDialog($"Done. Check the drawings.\nProcessed {drawings.Count} drawings in {(double)sw.ElapsedMilliseconds / 1000:0.###}s"); }

        }
    }
}
