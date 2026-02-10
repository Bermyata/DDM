using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using IsoDataManager.Commands;
using OfficeOpenXml;

namespace IsoDataManager
{
    public class ExcelReader
    {
        public static List<Drawing> ReadData( String currentDirectory, Logger logger, out Boolean hasError )
        {
            Dictionary<String, Dictionary<String, List<Block>>> drawings = new();

            var fi = new FileInfo( currentDirectory + Path.DirectorySeparatorChar + Settings.FileName );

            hasError = false;

            using ( var package = new ExcelPackage( fi ) )
            {
                ExcelWorksheets wss = package.Workbook.Worksheets;

                foreach ( ExcelWorksheet worksheet in wss )
                {
                    try
                    {
                        String blockNameTemplate = GetBlockName( worksheet );

                        if ( blockNameTemplate is null ) continue;

                        ExcelRange cells = worksheet.Cells;

                        Dictionary<String, Int32> headers = GetHeaders( worksheet );

                        if ( headers.Count < 3 ) continue;

                        for ( var row = 2; row <= worksheet.Dimension.Rows; row++ )
                        {
                            String fileName = cells[row, 1].GetValue<String>( );

                            String blockName = cells[row, 2].GetValue<String>( );

                            String suffix = blockNameTemplate.Contains( "*" )
                                ? blockName.Substring( blockName.Length - 2 )
                                : "";

                            Dictionary<String, String> attributes = new();

                            String attributeTag   = String.Empty;
                            String attributeValue = String.Empty;

                            try
                            {
                                foreach ( KeyValuePair<String, Int32> keyValuePair in headers )
                                {
                                    if ( keyValuePair.Value < 3 ) continue;

                                    attributeTag   = keyValuePair.Key + suffix;
                                    attributeValue = cells[row, keyValuePair.Value].GetValue<String>( );
                                    attributes.Add( attributeTag, attributeValue );
                                }

                                var blc = new Block( blockName, attributes );

                                if ( !drawings.TryGetValue( fileName, out Dictionary<String, List<Block>> blocks ) )
                                    drawings.Add( fileName
                                                , new Dictionary<String, List<Block>>
                                                  {
                                                      { blockNameTemplate, new List<Block> { blc } }
                                                  } );
                                else if ( !blocks.TryGetValue( blockNameTemplate, out List<Block> blocksList ) )
                                    blocks.Add( blockNameTemplate, new List<Block> { blc } );
                                else
                                    blocksList.Add( blc );
                            }
                            catch ( Exception e )
                            {
                                logger.Error(
                                    $"Reading data from Excel file. Block: {blockName}, Attribute: {attributeTag}, "
                                  + $"Value: {attributeValue}{Environment.NewLine}{e.Message}" );

                                hasError = true;
                            }
                        }
                    }
                    catch ( Exception e )
                    {
                        logger.Error(
                            $"Reading data from Excel file. Worksheet: {worksheet.Name}{Environment.NewLine}{e.Message}" );

                        hasError = true;
                    }
                }
            }

            return drawings.Select( x => new Drawing( x.Key, x.Value ) ).ToList( );
        }

        private static String GetBlockName( ExcelWorksheet worksheet )
        {
            String blockName = Settings.Names.FirstOrDefault( x => x.EqualsOrdinalIgnoreCase( worksheet.Name ) );

            blockName ??= Settings.Names.Where( x => x.Contains( "*" ) )
                                  .FirstOrDefault( x => x.Replace( "*", "" ).Equals( worksheet.Name ) );

            return blockName;
        }

        private static Dictionary<String, Int32> GetHeaders( ExcelWorksheet ws )
        {
            Dictionary<String, Int32> result = new();

            for ( var i = 3; i <= ws.Dimension.Columns; i++ ) result.Add( ws.Cells[1, i].GetValue<String>( ), i );

            return result;
        }
    }
}