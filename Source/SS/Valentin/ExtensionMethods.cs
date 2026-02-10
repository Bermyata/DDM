using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace IsoDataManager
{
    public static class ExtensionMethods
    {
        #region Static fields and Consts

        private static readonly RXClass attDefClass = RXObject.GetClass( typeof( AttributeDefinition ) );

        #endregion

        public static void SynchronizeAttributes( this BlockTableRecord target, Transaction tr )
        {
            if ( target == null ) return;

            if ( tr == null ) return;

            List<AttributeDefinition> attDefs = target.GetAttributes( tr );

            foreach ( ObjectId id in target.GetBlockReferenceIds( true, false ) )
            {
                var br = ( BlockReference ) tr.GetObject( id, OpenMode.ForWrite );
                br.ResetAttributes( attDefs, tr );
            }

            if ( target.IsDynamicBlock )
            {
                target.UpdateAnonymousBlocks( );

                foreach ( ObjectId id in target.GetAnonymousBlockIds( ) )
                {
                    var btr = ( BlockTableRecord ) tr.GetObject( id, OpenMode.ForRead );
                    attDefs = btr.GetAttributes( tr );

                    foreach ( ObjectId brId in btr.GetBlockReferenceIds( true, false ) )
                    {
                        var br = ( BlockReference ) tr.GetObject( brId, OpenMode.ForWrite );
                        br.ResetAttributes( attDefs, tr );
                    }
                }
            }
        }

        private static List<AttributeDefinition> GetAttributes( this BlockTableRecord target, Transaction tr )
        {
            var attDefs = new List<AttributeDefinition>( );

            foreach ( ObjectId id in target )
                if ( id.ObjectClass == attDefClass )
                {
                    var attDef = ( AttributeDefinition ) tr.GetObject( id, OpenMode.ForRead );
                    attDefs.Add( attDef );
                }

            return attDefs;
        }

        private static void ResetAttributes( this BlockReference br, List<AttributeDefinition> attDefs, Transaction tr )
        {
            var attValues = new Dictionary<String, (String, AttachmentPoint )>( );

            foreach ( ObjectId id in br.AttributeCollection )
                if ( !id.IsErased )
                {
                    var attRef = ( AttributeReference ) tr.GetObject( id, OpenMode.ForWrite );

                    var v = attRef.IsMTextAttribute ? attRef.MTextAttribute.Contents : attRef.TextString;
                    attValues.Add( attRef.Tag,(v,attRef.Justify)  );

                    attRef.Erase( );
                }

            foreach ( AttributeDefinition attDef in attDefs )
            {
                var attRef = new AttributeReference( );
                attRef.SetAttributeFromBlock( attDef, br.BlockTransform );

                if ( attDef.Constant )
                    attRef.TextString = attDef.IsMTextAttributeDefinition
                        ? attDef.MTextAttributeDefinition.Contents
                        : attDef.TextString;
                else if ( attValues.ContainsKey( attRef.Tag ) )
                {
                    attRef.TextString = attValues[attRef.Tag].Item1;
                    attRef.Justify = attValues[attRef.Tag].Item2;
                    attRef.Height = 2.5;
                }

                br.AttributeCollection.AppendAttribute( attRef );
                tr.AddNewlyCreatedDBObject( attRef, true );
            }
        }
    }
}