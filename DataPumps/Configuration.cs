using System;
using System.Collections.Generic;

namespace DataPumps
{
    public class Configuration
    {
        public Location Source { get; set; }
        public Location Destination { get; set; }
    }

    public class Location
    {
        public LocationType Type { get; set; }
        public String Path { get; set; }
        public String Delimiter { get; set; }
        public IEnumerable<Column> Columns { get; set; }
    }

    public class Column
    {
        public String Name { get; set; }
        public Int32 Order { get; set; }
        public DataType Type { get; set; }
    }

    public class SourceColumn : Column
    {
        public Boolean Skip { get; set; }
    }
}