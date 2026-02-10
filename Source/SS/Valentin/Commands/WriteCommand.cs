using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using IsoDataManager.Commands;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Exception = System.Exception;

namespace IsoDataManager
{
    public class WriteCommand
    {
        [CommandMethod( "VTWriteToIso", CommandFlags.Session )]
        public static void Read( )
        {
            var sw = new Stopwatch( );

            sw.Start( );

            DocumentCollection acDocMgr = Application.DocumentManager;

            String filePath = acDocMgr.MdiActiveDocument.Name;

            String currentDirectory = Path.GetDirectoryName( filePath );

            Logger logger = new Logger( currentDirectory );

            if ( !File.Exists( currentDirectory + Path.DirectorySeparatorChar + Settings.FileName ) )
            {
                Application.ShowAlertDialog( $"{Settings.FileName} is not found in directory: {currentDirectory}" );
                logger.Warn( $"{Settings.FileName} is not found in directory: {currentDirectory}" );
                return;
            }

            List<Drawing> drawings = ExcelReader.ReadData( currentDirectory, logger, out Boolean hasErrors );

            if ( hasErrors )
            {
                String directory = AppDomain.CurrentDomain.BaseDirectory;

                Application.ShowAlertDialog(
                    $"Errors during read {Settings.FileName}{Environment.NewLine}Check log file in the directory: {directory}" );

                return;
            }

            acDocMgr.MdiActiveDocument.CloseAndDiscard( );

            foreach ( Drawing drw in drawings )
            {
                String drwPath = currentDirectory + Path.DirectorySeparatorChar + drw.Path;

                if ( !File.Exists( drwPath ) ) continue;

                try
                {
                    using ( var db = new Database( false, false ) )
                    {
                        db.ReadDwgFile( drwPath, FileShare.ReadWrite, true, String.Empty );

                        hasErrors = WriteService.WriteBlocks( db
                                                            , drw.Blocks.SelectMany( x => x.Value ).ToList( )
                                                            , logger );

                        if ( hasErrors ) logger.Error( $"Error ABOVE occured in Drawing: {drw.Path}" );

                        db.SaveAs( drwPath, DwgVersion.Current );
                    }
                }
                catch ( Exception e )
                {
                    logger.Error( $"Error during writing Drawing: {drw.Path}{Environment.NewLine}{e.Message}" );
                    hasErrors = true;
                }
            }

            sw.Stop( );

            acDocMgr.Open( currentDirectory + Path.DirectorySeparatorChar + drawings.First( ).Path );

            if ( hasErrors )
            {
                String directory = AppDomain.CurrentDomain.BaseDirectory;
                Application.ShowAlertDialog( $"Errors occured. Check log file in the directory: {directory}" );
            }
            else { Application.ShowAlertDialog( "No errors" ); }

            logger.Info( $"Write Command Performance: processed {drawings.Count} drawings in {(double)sw.ElapsedMilliseconds /1000:0.###}s" );
        }
    }
}