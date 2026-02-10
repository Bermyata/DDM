using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.IO;
using System.Linq;

namespace DrawingDataManager
{

    public class BlockNameSaver
    {
        [CommandMethod("DDM_GET_BLOCKS_NAME")]
        public void GetBlockNames()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Prompt user to select blocks
            PromptSelectionResult selRes = ed.GetSelection(new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") }));

            if (selRes.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Get the current drawing file path
                    string fileName = Path.Combine(Path.GetDirectoryName(db.Filename), "Blocks.txt");

                    // Open file for writing
                    using (StreamWriter sw = new StreamWriter(fileName))
                    {
                        // Iterate through selected blocks
                        foreach (SelectedObject selObj in selRes.Value)
                        {
                            if (selObj != null)
                            {
                                BlockReference blkRef = (BlockReference)tr.GetObject(selObj.ObjectId, OpenMode.ForRead);
                                string blockName = blkRef.Name;

                                // Write block name to file
                                sw.WriteLine(blockName);
                            }
                        }
                    }

                    tr.Commit();
                    ed.WriteMessage($"\nBlock names saved to: {fileName}");
                }
            }
            else
            {
                ed.WriteMessage("\nNo blocks selected.");
            }
        }
    }
}