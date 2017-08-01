using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Buffer = DataPumps.Buffer;

namespace DataPumps.Test
{
    [TestClass]
    public class BufferTests
    {
        [TestMethod]
        public void Buffer_Should_Be_Empty_When_Created()
        {
            var buffer = new Buffer();
            Assert.IsTrue(buffer.IsEmpty);
        }

        [TestMethod]
        public void Write_Should_Add_Data_To_The_Buffer()
        {
            var buffer = new Buffer();
            buffer.Write("test");
            Assert.AreEqual(1, buffer.Content.Count);
            Assert.AreEqual("test", buffer.Content.Peek());
        }

        [TestMethod]
        public void Write_Should_Throw_Error_When_Buffer_Is_Full()
        {
            var buffer = new Buffer(new BufferOptions { Size = 1});
            buffer.Write("test");
            Assert.ThrowsException<InvalidOperationException>(() => buffer.Write("agian"));
        }

        [TestMethod]
        public void Write_Should_Emit_Full_Event_When_Buffer_Becomes_Full()
        {
            var buffer = new Buffer(new BufferOptions { Size = 2});
            var full = false;
            buffer.Write("test");
            buffer.FullEvent += () => full = true;

            buffer.Write("test");
            Assert.IsTrue(full);
        }

        [TestMethod]
        public async Task WriteAsync_Should_Write_To_Buffer_When_Not_Full()
        {
            var buffer = new Buffer(new BufferOptions {Size = 1});

            await buffer
                .WriteAsync("test")
                .ContinueWith(_ => Assert.AreEqual(1, buffer.Content.Count));
        }

        [TestMethod]
        public async Task WriteAsync_Should_Wait_For_Read_Event_To_Write_The_Buffer()
        {
            var buffer = new Buffer(new BufferOptions { Size = 1 });

            buffer.Write("test1");

            var promise = buffer
                .WriteAsync("test2")
                .ContinueWith(_ =>
                {
                    Assert.AreEqual(1, buffer.Content.Count);
                    Assert.AreEqual("test2", buffer.Content.Peek());
                });

            buffer.Read();
            await promise;
        }

        [TestMethod]
        public void Read_Should_Return_First_data_Item_When_Not_Empty()
        {
            var buffer = new Buffer();

            buffer.Write("test1");
            buffer.Write("test2");
            Assert.AreEqual("test1", buffer.Read());
        }

        [TestMethod]
        public void Read_Should_Throw_Error_When_Buffer_Is_Empty()
        {
            var buffer = new Buffer();

            Assert.ThrowsException<InvalidOperationException>(() => buffer.Read());
        }

        [TestMethod]
        public async Task ReadAsync_Should_Return_Completed_Task_When_Readable()
        {
            var buffer = new Buffer();

            buffer.Write("test");

            await buffer.ReadAsync().ContinueWith(t => Assert.AreEqual("test", t.Result));
        }
    }
}