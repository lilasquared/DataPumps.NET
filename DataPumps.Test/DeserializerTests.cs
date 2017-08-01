using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataPumps.Test
{
    [TestClass]
    public class DeserializerTests
    {
        private class SimpleObject
        {
            public String Name { get; set; }
        }

        private String TestSimpleObject = @"
            name: Test
";

        [TestMethod]
        public void Deserializer_CanDeserialize_SimpleYaml()
        {
            var deserializer = new Deserializer();

            var obj = deserializer.Deserialize<SimpleObject>(TestSimpleObject);

            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Name);
            Assert.AreEqual(obj.Name, "Test");
        }

        private String TestConfiguration = @"
source:
    type: file
    path: file1

destination:
    type: file
    path: file2

map:
    - 
        columnName : Id
    -
        columnName : Name
";

        [TestMethod]
        public void Deserializer_CanDeserialize_Configuration()
        {
            var deserializer = new Deserializer();

            var obj = deserializer.Deserialize<Configuration>(TestConfiguration);

            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Source);
            Assert.AreEqual(obj.Source.Type, LocationType.File);
        }
    }
}