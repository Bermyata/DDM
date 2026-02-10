using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DrawingDataManager.Commands;
using OfficeOpenXml;

namespace DrawingDataManager
{
    public class ExcelWriter
    {
        public static bool WriteData( string currentDirectory, IEnumerable<Drawing> drawings, Logger logger)
        {
            bool hasErrors = false;

            var fi = new FileInfo( SFileNameService.GetFileName(
                                       currentDirectory
                                     + Path.DirectorySeparatorChar
                                     + Path.GetFileNameWithoutExtension( Settings.FileName )
                                     , Path.GetExtension( Settings.FileName ) ) );

            List<List<(String Path, String Key, List<Block> Value)>> byBlock = drawings
                                                                              .SelectMany( x => x.Blocks.Select( y => ( x.Path, y.Key, y.Value ) ) )
                                                                              .GroupBy(x=>x.Key)
                                                                              .Select(x=>x.ToList())
                                                                              .ToList( );

            try
            {
                using ( var package = new ExcelPackage( ) )
                {
                    foreach ( List<(String Path, String Key, List<Block> Value)> grp in byBlock )
                    {
                        var first = grp[0];

                        bool hasAsterisk = first.Key.Contains( "*" );

                        ExcelWorksheet ws = package.Workbook.Worksheets.Add( first.Key.Replace("*" , "") );

                        List<string> uniqueAttributeNames =
                            grp.SelectMany( x => x.Value.SelectMany( y => y.Attributes.Select( z => hasAsterisk ? z.Key.Substring(0, z.Key.Length-2)  : z.Key ) ) )
                               .Distinct( ).ToList( );

                        Dictionary<string, int> headers = GetHeaders( uniqueAttributeNames );

                        foreach ( var value in headers ) ws.Cells[1, value.Value].Value = value.Key;

                        int row = 2;

                        foreach ( (String Path, String Key, List<Block> Value) drw in grp )
                        {
                            foreach ( Block block in drw.Value )
                            {
                                ws.Cells[row, 1].Value = drw.Path;

                                foreach ( var attribute in block.Attributes )
                                {
                                    try
                                    {
                                        var key = hasAsterisk
                                            ? attribute.Key.Substring( 0, attribute.Key.Length - 2 )
                                            : attribute.Key;

                                        ws.Cells[row, headers[key]].Value = attribute.Value;
                                    }
                                    catch ( Exception e )
                                    {
                                       logger.Error($"Writing block attribute data to Excel document. Drawing: {drw.Path}, Block: {block.Name}, attribute {attribute.Key}\n{e.Message}");
                                       hasErrors = true;
                                    }
                                }

                                row++;
                            }
                        }
                    }

                    package.SaveAs( fi );
                }
            }
            catch(Exception e)
            {
                logger.Error($"Writing Excel document outer error{Environment.NewLine}{e.Message}");
                hasErrors = true;
            }

            return hasErrors;
        }

        private static Dictionary<string,int> GetHeaders( List<string> uniqueAttributeNames )
        {
            List<string> attributeNames = new List<String> { "__FileName"};

            attributeNames.AddRange(uniqueAttributeNames);

            Dictionary<string, int> result = new();

            int i = 1;

            foreach ( string attributeName in attributeNames )
                result.Add(attributeName,i++);

            return result;
        }
    }
}