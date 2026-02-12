using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using DrawingDataManager.Commands;
using OfficeOpenXml;

namespace DrawingDataManager
{
    public class ExcelReader
    {
        public static List<Drawing> ReadData(String currentDirectory, Logger logger, out Boolean hasError)
        {
            Dictionary<String, Dictionary<String, List<Block>>> drawings = new();

            var blockNamesFile = currentDirectory + Path.DirectorySeparatorChar + Settings.FileBlockName;
            if (!File.Exists(blockNamesFile))
            {
                logger.Error($"{Settings.FileBlockName} is not found in directory: {currentDirectory}");
                hasError = true;
                return new List<Drawing>();
            }

            Settings.InitializeNames(blockNamesFile);
            var fi = new FileInfo(currentDirectory + Path.DirectorySeparatorChar + Settings.FileName);

            hasError = false;

            using (var package = new ExcelPackage(fi))
            {
                ExcelWorksheets wss = package.Workbook.Worksheets;

                foreach (ExcelWorksheet worksheet in wss)
                {
                    try
                    {
                        String blockNameTemplate = GetBlockName(worksheet);

                        if (blockNameTemplate is null) continue;
                        if (worksheet.Dimension == null) continue;

                        ExcelRange cells = worksheet.Cells;

                        Dictionary<String, Int32> headers = GetHeaders(worksheet);

                        if (headers.Count < 2) continue;

                        for (var row = 2; row <= worksheet.Dimension.Rows; row++)
                        {
                            String fileName = cells[row, 1].GetValue<String>();

                            String blockNameId = cells[row, 2].GetValue<String>();
                            if (String.IsNullOrWhiteSpace(fileName) || String.IsNullOrWhiteSpace(blockNameId)) continue;

                            // String suffix = blockNameTemplate.Contains("*") ? blockName.Substring(blockName.Length - 2) : "";

                            Dictionary<String, String> attributes = new();

                            String attributeTag = String.Empty;
                            String attributeValue = String.Empty;

                            try
                            {
                                foreach (KeyValuePair<String, Int32> keyValuePair in headers)
                                {

                                    if (keyValuePair.Value < 3) continue;

                                    attributeTag = keyValuePair.Key;// + suffix;
                                    attributeValue = cells[row, keyValuePair.Value].GetValue<String>();
                                    attributes.Add(attributeTag, attributeValue);
                                }

                                var blc = new Block(blockNameId, attributes);

                                if ( !drawings.TryGetValue( fileName, out Dictionary<String, List<Block>> blocks ) )
                                    drawings.Add( fileName
                                                , new Dictionary<String, List<Block>>
                                                  {
                                                      { blockNameId, new List<Block> { blc } }
                                                  } );
                                else if ( !blocks.TryGetValue( blockNameId, out List<Block> blocksList ) )
                                    blocks.Add( blockNameId, new List<Block> { blc } );
                                else
                                    blocksList.Add( blc );

                            }
                            catch (Exception e)
                            {
                                logger.Error($"Reading data from Excel file. Block: {blockNameId}, Attribute: {attributeTag}, Value: {attributeValue}{Environment.NewLine}{e.Message}");

                                hasError = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Reading data from Excel file. Worksheet: {worksheet.Name}{Environment.NewLine}{e.Message}");

                        hasError = true;
                    }
                }
            }

            return drawings.Select(x => new Drawing(x.Key, x.Value)).ToList();
        }


        private static String GetBlockName(ExcelWorksheet worksheet)
        {

            String blockName = Settings.Names.FirstOrDefault(x => x.EqualsOrdinalIgnoreCase(worksheet.Name));

            blockName ??= Settings.Names.Where(x => x.Contains("*"))
                                  .FirstOrDefault(x => x.Replace("*", "").Equals(worksheet.Name));

            return blockName;
        }

        private static Dictionary<String, Int32> GetHeaders(ExcelWorksheet ws)
        {
            Dictionary<String, Int32> result = new();

            for (var i = 2; i <= ws.Dimension.Columns; i++)
            {
                var header = ws.Cells[1, i].GetValue<String>();
                if (String.IsNullOrWhiteSpace(header)) continue;
                if (!result.ContainsKey(header)) result.Add(header, i);
            }

            return result;
        }
    }
}
