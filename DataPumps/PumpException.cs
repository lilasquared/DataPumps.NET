using System;

namespace DataPumps
{
    public class PumpException
    {
        public String Error { get; }
        public String Pump { get; }

        public PumpException(String error, String pump)
        {
            Error = error;
            Pump = pump;
        }
    }
}