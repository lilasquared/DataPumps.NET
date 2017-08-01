using System;
using System.Collections.Generic;

namespace DataPumps
{
    public class BufferOptions
    {
        public IEnumerable<Object> Content { get; set; }
        public Int32? Size { get; set; }
        public Boolean? Sealed { get; set; }
    }
}