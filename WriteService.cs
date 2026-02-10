using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using DrawingDataManager.Commands;
using Bushman.AutoCAD.DatabaseServices;


namespace DrawingDataManager

{
    public class AttributeProperties
    {
        public double WidthFactor { get; set; }
        public ObjectId TextStyle { get; set; }
        public AttachmentPoint Justify { get; set; }
        public double Height { get; set; }
        public double Rotation { get; set; }
        public AnnotativeStates Annotative { get; set; }
        public double Oblique { get; set; }
    }
    public class WriteService
    {
        public static bool WriteBlocks(Database db, List<Block> blocks, Logger logger)
        {
            bool hasErrors = false;

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
                        String blockName = AcadHelper.GetAcadBlockNameId(brf);
                        String blockName1 = AcadHelper.GetAcadBlockName(brf);

                        var required = false;
                        Block block = null;

                        foreach (Block blc in blocks)
                            if (blockName.EqualsOrdinalIgnoreCase(blc.Name))
                            {
                                required = true;
                                block = blc;
                                break;
                            }

                        if (!required) continue;

                        string attributeTag = String.Empty;
                        string value_vis = string.Empty;
                        string value = string.Empty;
                        try
                        {




                            Dictionary<ObjectId, AttributeProperties> originalProperties = new Dictionary<ObjectId, AttributeProperties>();

                            AttributeCollection attCol = brf.AttributeCollection;
                            foreach (ObjectId attributeId in attCol)
                            {
                                var attRef = (AttributeReference)transaction.GetObject(attributeId, OpenMode.ForWrite);




                                if (block.Attributes.TryGetValue(attRef.Tag, out value))
                                {
                                    originalProperties[attributeId] = new AttributeProperties
                                    {
                                        WidthFactor = attRef.WidthFactor,
                                        TextStyle = attRef.TextStyleId,
                                        Justify = attRef.Justify,
                                        Height = attRef.Height,
                                        Rotation = attRef.Rotation,
                                        Annotative = attRef.Annotative,
                                        Oblique = attRef.Oblique
                                    };
                                    try
                                    {
                                        attRef.TextString = value;
                                    }
                                    catch (Exception)
                                    {
                                        attRef.TextString = string.Empty;
                                    }
                                }
                            }

                            BlockTable btt = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);

                            BlockReference brr = (BlockReference)transaction.GetObject(objectId, OpenMode.ForRead);

                            BlockTableRecord btr = (BlockTableRecord)transaction.GetObject(btt[brr.Name], OpenMode.ForRead);

                            btr.AttSync(false, false, false);


                            foreach (ObjectId attributeId in attCol)
                            {
                                var attRef = (AttributeReference)transaction.GetObject(attributeId, OpenMode.ForWrite);

                                if (originalProperties.TryGetValue(attributeId, out AttributeProperties properties))
                                {
                                    try
                                    {
                                        attRef.WidthFactor = properties.WidthFactor;
                                    }
                                    catch (Exception)
                                    { }

                                    try
                                    {
                                        attRef.TextStyleId = properties.TextStyle;
                                    }
                                    catch (Exception)
                                    { }

                                    try
                                    {
                                        attRef.Justify = properties.Justify;
                                    }
                                    catch (Exception)
                                    { }

                                    try
                                    {
                                        attRef.Height = properties.Height;
                                    }
                                    catch (Exception)
                                    { }

                                    try
                                    {
                                        attRef.Rotation = properties.Rotation;
                                    }
                                    catch (Exception)
                                    { }

                                    try
                                    {
                                        attRef.Annotative = properties.Annotative;
                                    }
                                    catch (Exception)
                                    { }

                                    try
                                    {
                                        attRef.Oblique = properties.Oblique;
                                    }
                                    catch (Exception)
                                    { }
                                }
                            }



                            if (brf.IsDynamicBlock)
                            {
                                DynamicBlockReferencePropertyCollection viscoll = brf.DynamicBlockReferencePropertyCollection;

                                if (viscoll.Count != 0)
                                {
                                    foreach (DynamicBlockReferenceProperty visibilityId in viscoll)
                                    {

                                        if (block.Attributes.TryGetValue(visibilityId.PropertyName, out value_vis))

                                            visibilityId.Value = value_vis;

                                        break;
                                    }
                                }
                            }



                            // modelSpace.AttSync(false, true, false);

                        }

                        catch (Exception e)
                        {
                            logger.Error($"Error during Setting attribute value. Block: {brf.Name}, Attribute: {attributeTag}, Value: {value}{Environment.NewLine}{e.Message}");
                            hasErrors = true;
                        }
                    }
                }

                transaction.Commit();
            }

            return hasErrors;
        }

    }

}