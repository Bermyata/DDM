using System;
using System.IO;

namespace DrawingDataManager
{
    public static class SFileNameService
    {
        public static String GetFileName( String fileNameFull, String extension )
        {
            String fileName  = Path.GetFileNameWithoutExtension( fileNameFull );
            String directory = Path.GetDirectoryName( fileNameFull );
            String tempName;
            String counter = String.Empty;
            var    i       = 1;

            while ( File.Exists( tempName = directory + Path.DirectorySeparatorChar + fileName + counter + extension ) )
                counter = $"_({i++})";

            return tempName;
        }
    }
}