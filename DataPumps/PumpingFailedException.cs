using System;

namespace DataPumps
{
    public class PumpingFailedException : Exception
    {
        public PumpingFailedException(
            String message = "Pumping failed. See .ErrorBuffer() contents for error messages") 

            : base(message) { }
    }
}