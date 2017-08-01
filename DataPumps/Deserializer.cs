using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DataPumps
{
    public class Deserializer
    {
        public T Deserialize<T>(String input) where T : new()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            return deserializer.Deserialize<T>(input);
        }
    }
}