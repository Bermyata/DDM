using System;
using System.Collections.Generic;

namespace DrawingDataManager
{
    public record Drawing( String Path, Dictionary<String, List<Block>> Blocks );
}