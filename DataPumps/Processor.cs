using System;
using System.IO;

namespace DataPumps
{
    public interface IGenerator
    {
        T Generate<T>(String s);
    }

    public class IntegerGenerator : IGenerator
    {
        public T Generate<T>(String s)
        {
            return (T) Convert.ChangeType(s, typeof(T));
        }
    }

    public interface IGeneratorFactory
    {
        IGenerator GetGenerator();
    }

    public enum DataType
    {
        String,
        Integer,
        Double
    }

    public class Processor
    {

        private readonly Configuration _config;
        public Processor(Configuration config)
        {
            _config = config;
        }

        public void Process()
        {
            var file = File.ReadAllLines(_config.Source.Path);

            var sourceColumns = _config.Source.Columns;
        }

        public T Generate<T>(String s)
        {
            return (T)Convert.ChangeType(s, typeof(T));
        }
    }
}