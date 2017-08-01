using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataPumps.Test
{
    [TestClass]
    public class ProcessorTests
    {
        [TestMethod]
        public void Processor()
        {
            var config = new Configuration
            {
                Source = new Location
                {
                    Type = LocationType.File,
                    Path = "file1"
                },
            };

            var processor = new Processor(config);
            processor.Process();
        }
    }
}