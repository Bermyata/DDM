using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace IsoDataManager
{
    public class AcadHelper
    {
        public static void ChangeLayer( ObjectId[] children
                                      , String layerName
                                      , Boolean freeze = false
                                      , Byte transparent = 0 )
        {
            ObjectId layerId = CreateFindLayer( layerName, freeze, transparent );

            using ( Transaction transaction = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager
                                                         .StartTransaction( ) )
            {
                Parallel.ForEach( children
                                , x =>
                                  {
                                      Debug.Assert( transaction != null, nameof( transaction ) + " != null" );

                                      var brf = transaction.GetObject( x, OpenMode.ForWrite ) as BlockReference;

                                      Debug.Assert( brf != null, nameof( brf ) + " != null" );

                                      brf.LayerId = layerId;
                                  } );

                transaction.Commit( );
            }
        }

        public static String GetAcadBlockName( BlockReference blRef )
        {
            String name = blRef.IsDynamicBlock
                ? ( blRef.DynamicBlockTableRecord.GetObject( OpenMode.ForRead ) as BlockTableRecord )?.Name ?? ""
                : ( blRef.BlockTableRecord.GetObject( OpenMode.ForRead ) as BlockTableRecord )?.Name        ?? "";

            return name;
        }

        public static String GetAttributeValue( ObjectId id, String tag )
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            using ( Transaction transaction = db.TransactionManager.StartTransaction( ) )
            {
                var blockReference = transaction.GetObject( id, OpenMode.ForRead ) as BlockReference;

                Debug.Assert( blockReference != null, nameof( blockReference ) + " != null" );

                AttributeCollection attCol = blockReference.AttributeCollection;

                foreach ( ObjectId attributeId in attCol )
                {
                    var attRef = ( AttributeReference ) transaction.GetObject( attributeId, OpenMode.ForRead );

                    if ( attRef.Tag.EqualsOrdinalIgnoreCase( tag ) ) return attRef.TextString;
                }
            }

            return String.Empty;
        }

        public static String GetBoxName( ObjectId id ) { return GetAttributeValue( id, "NAME" ); }

        public static PromptResult SimplePrompt( String message, String defaultOption, params String[] keys )
        {
            var keysWithDefault = new String[keys.Length + 1];
            keysWithDefault[0] = defaultOption;

            Array.Copy( keys, 0, keysWithDefault, 1, keys.Length );

            if ( !keysWithDefault.Any( ) ) return default;

            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

            var pKeyOpts = new PromptKeywordOptions( "" ) { Message = "\n" + message + " :" };

            foreach ( String key in keys ) pKeyOpts.Keywords.Add( key );

            pKeyOpts.Keywords.Default = keys[0];

            pKeyOpts.AllowNone = false;

            PromptResult pKeyRes = editor.GetKeywords( pKeyOpts );

            return pKeyRes;
        }

        internal static void PrintConnectionResult( IEnumerable<Double> currents )
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
            editor.WriteMessage( $"\nTotal Startup on MB: {currents.Sum( x => x )}" );
            var i = 1;
            foreach ( Double current in currents ) editor.WriteMessage( $"\nPhase {i++}: {current:0.##}" );
        }

        private static ObjectId CreateFindLayer( String layerName, Boolean freeze, Byte transparent )
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            layerName = String.IsNullOrWhiteSpace( layerName ) ? "0" : layerName;

            ObjectId layerTableId;

            using ( Transaction transaction = db.TransactionManager.StartTransaction( ) )
            {
                var lt = transaction.GetObject( db.LayerTableId, OpenMode.ForRead ) as LayerTable;

                Debug.Assert( lt != null, nameof( lt ) + " != null" );

                if ( lt.Has( layerName ) ) return lt[layerName];

                var layerTableRecord = new LayerTableRecord
                {
                    Name         = layerName
                  , Color        = Color.FromColorIndex( ColorMethod.ByAci, 0 )
                  , IsFrozen     = freeze
                  , Transparency = new Transparency( transparent )
                };

                lt.UpgradeOpen( );
                layerTableId = lt.Add( layerTableRecord );
                transaction.AddNewlyCreatedDBObject( layerTableRecord, true );
                transaction.Commit( );
            }

            // using ( Transaction transaction = db.TransactionManager.StartTransaction( ) )
            // {
            //     var lt               = transaction.GetObject( db.LayerTableId, OpenMode.ForRead ) as LayerTable;
            //     var layerTableRecord = transaction.GetObject( lt[layerName], OpenMode.ForWrite ) as LayerTableRecord;
            //     layerTableRecord.Transparency = new Transparency( transparent );
            //     transaction.Commit( );
            // }

            return layerTableId;
        }
    }
}