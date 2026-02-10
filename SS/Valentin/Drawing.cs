using System;
using System.Collections.Generic;

namespace IsoDataManager
{
    public record Drawing( String Path, Dictionary<String, List<Block>> Blocks );
}