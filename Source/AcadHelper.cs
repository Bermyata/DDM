using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace DrawingDataManager
{
    public class AcadHelper
    {
        public static String GetAcadBlockName(BlockReference blRef)
        {
            String name = blRef.IsDynamicBlock
                ? (blRef.DynamicBlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord)?.Name ?? ""
                : (blRef.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord)?.Name ?? "";
            return name;

        }

        public static String GetAcadBlockNameId(BlockReference blRef)
        {
            String name = blRef.IsDynamicBlock
                ? (blRef.DynamicBlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord)?.Name+blRef.Handle.ToString() ?? ""
                : (blRef.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord)?.Name+blRef.Handle.ToString() ?? "";
            return name;

        }

        public static String GetAttributeValue(ObjectId id, String tag)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                var blockReference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;

                Debug.Assert(blockReference != null, nameof(blockReference) + " != null");

                AttributeCollection attCol = blockReference.AttributeCollection;

                foreach (ObjectId attributeId in attCol)
                {
                    var attRef = (AttributeReference)transaction.GetObject(attributeId, OpenMode.ForRead);

                    if (attRef.Tag.EqualsOrdinalIgnoreCase(tag)) return attRef.TextString;
                }
            }

            return String.Empty;
        }
    }
}