using System;
using System.IO;

namespace IsoDataManager.Commands
{
    public class Logger
    {
        private readonly String _logFileName = "log.txt";

        public Logger( String currentDirectory )
        {
            _logFileName = currentDirectory + Path.DirectorySeparatorChar + _logFileName;
        }

        public void Error( String s )
        {
            using ( var sw = new StreamWriter( _logFileName, true ) )
            {
                DateTime date = DateTime.Now;
                sw.WriteLine( $"{date} ERROR: {s}" );
            }
        }

        public void Info( String s )
        {
            using ( var sw = new StreamWriter( _logFileName, true ) )
            {
                DateTime date = DateTime.Now;
                sw.WriteLine( $"{date} INFO: {s}" );
            }
        }

        public void Warn( String s )
        {
            using ( var sw = new StreamWriter( _logFileName, true ) )
            {
                DateTime date = DateTime.Now;
                sw.WriteLine( $"{date} WARN: {s}" );
            }
        }
    }
}