using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using DrawingDataManager.Commands;

namespace DrawingDataManager
{
    public class ReadService
    {
        public static Dictionary<String, List<Block>> ReadBlocks(Database db, IEnumerable<String> blockNames, Logger logger)
        {
            Dictionary<String, List<Block>> result = new();

            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                Debug.Assert(blockTable != null, nameof(blockTable) + " != null");

                var modelSpace =
                    transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as
                        BlockTableRecord;

                Debug.Assert(modelSpace != null, nameof(modelSpace) + " != null");

                foreach (ObjectId objectId in modelSpace)
                {
                    if (!objectId.ObjectClass.DxfName.EqualsOrdinalIgnoreCase("Insert")) continue;

                    if (transaction.GetObject(objectId, OpenMode.ForRead) is BlockReference brf)
                    {
                        String blockName = AcadHelper.GetAcadBlockName(brf);
                        String blockNameId = AcadHelper.GetAcadBlockNameId(brf);

                        var required = false;
                        String blockNameTemplate = String.Empty;


                        foreach (String name in blockNames)
                            if (blockName.EqualsOrdinalIgnoreCase(name)
                              || name.EndsWith("*")
                              && blockName.Length > name.Length
                              && CheckAsterisk(name, blockName))
                            {
                                required = true;
                                blockNameTemplate = name;

                                if (!result.ContainsKey(blockNameTemplate)) result.Add(blockNameTemplate, new List<Block>());

                                break;
                            }

                        if (!required) continue;

                        Dictionary<String, String> attributes = new();
                        string attributeTag = string.Empty;

                        try
                        {

                            AttributeCollection attCol = brf.AttributeCollection;
                            attributes.Add("Handle", objectId.Handle.ToString());

                            if (brf.IsDynamicBlock)
                            {
                                DynamicBlockReferencePropertyCollection viscoll = brf.DynamicBlockReferencePropertyCollection;

                                if (viscoll.Count != 0)
                                {
                                    foreach (DynamicBlockReferenceProperty visibilityId in viscoll)
                                    {
                                        attributes.Add(visibilityId.PropertyName, visibilityId.Value.ToString());
                                    }
                                }
                            }

                            foreach (ObjectId attributeId in attCol)
                            {
                                var attRef =
                                    (AttributeReference)transaction.GetObject(attributeId, OpenMode.ForRead);

                                attributeTag = attRef.Tag;

                                if (!String.IsNullOrWhiteSpace(attRef.TextString))
                                    attributes.Add(attRef.Tag, attRef.TextString);


                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error($"Reading blocks error. Block: {blockName}, attribute: {attributeTag}{Environment.NewLine}{e.Message}");
                            throw;
                        }

                        if (attributes.Any()) result[blockNameTemplate].Add(new Block(blockNameId, attributes));
                    }
                }
            }

            return result.Where(x => x.Value.Any()).ToDictionary(x => x.Key, x => x.Value);

        }

        public static Boolean CheckAsterisk(String withAsterisk, String blockName)
        {
            Int32 length = withAsterisk.IndexOf("*");

            return withAsterisk.Substring(0, length).EqualsOrdinalIgnoreCase(blockName.Substring(0, length));
        }
    }
}
