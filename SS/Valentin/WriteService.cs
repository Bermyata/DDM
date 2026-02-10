using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using IsoDataManager.Commands;

namespace IsoDataManager
{
    public class WriteService
    {
        public static bool WriteBlocks( Database db, List<Block> blocks, Logger logger )
        {
            bool hasErrors = false;

            using ( Transaction transaction = db.TransactionManager.StartTransaction( ) )
            {
                var blockTable = transaction.GetObject( db.BlockTableId, OpenMode.ForRead ) as BlockTable;

                Debug.Assert( blockTable != null, nameof( blockTable ) + " != null" );

                var modelSpace =
                    transaction.GetObject( blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead ) as
                        BlockTableRecord;

                Debug.Assert( modelSpace != null, nameof( modelSpace ) + " != null" );

                foreach ( ObjectId objectId in modelSpace )
                {
                    if ( !objectId.ObjectClass.DxfName.EqualsOrdinalIgnoreCase( "Insert" ) ) continue;

                    if ( transaction.GetObject( objectId, OpenMode.ForRead ) is BlockReference brf )
                    {
                        String blockName = AcadHelper.GetAcadBlockName( brf );

                        var   required = false;
                        Block block    = null;

                        foreach ( Block blc in blocks )
                            if ( blockName.EqualsOrdinalIgnoreCase( blc.Name ) )
                            {
                                required = true;
                                block    = blc;
                                break;
                            }

                        if ( !required ) continue;

                        string attributeTag = String.Empty;
                        string value = string.Empty;

                        try
                        {
                            AttributeCollection attCol = brf.AttributeCollection;

                            foreach ( ObjectId attributeId in attCol )
                            {
                                var attRef = ( AttributeReference ) transaction.GetObject( attributeId, OpenMode.ForWrite );

                                if ( block.Attributes.TryGetValue( attRef.Tag, out  value ) )
                                {
                                    attRef.UpgradeOpen();
                                    attRef.TextString = value ?? string.Empty;
                                    attRef.AdjustAlignment(db);
                                    attRef.DowngradeOpen();
                                }
                            }
                        }
                        catch ( Exception e )
                        {
                            logger.Error($"Error during Setting attribute value. Block: {blockName}, Attribute: {attributeTag}, Value: {value}{Environment.NewLine}{e.Message}");
                            hasErrors = true;
                        }
                    }
                }

                transaction.Commit( );
            }

            return hasErrors;
        }
    }
}