using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataPumps.Test
{
    [TestClass]
    public class PumpTests
    {
        [TestMethod]
        public void Start_Should_Pump_Content_From_Source_To_Output_Buffer()
        {
            var buffer = new Buffer(new BufferOptions
            {
                Content = new[] {"foo", "bar", "test", "content"}
            });

            buffer.Seal();

            var pump = new Pump();
            pump.From(buffer);

            pump.Start();
        }
    }
}
