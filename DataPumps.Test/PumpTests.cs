using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataPumps.Test
{
    [TestClass]
    public class PumpTests
    {
        [TestMethod]
        public async Task Start_Should_Pump_Content_From_Source_To_Output_Buffer()
        {
            var buffer = new Buffer(new BufferOptions
            {
                Content = new[] {"foo", "bar", "test", "content"}
            });

            buffer.Seal();

            var pump = new Pump()
                .From(buffer);

            pump.Start();
            await pump.Buffer().ReadAsync().ContinueWith(task => Assert.AreEqual("foo", task.Result));
            await pump.Buffer().ReadAsync().ContinueWith(task => Assert.AreEqual("bar", task.Result));
            await pump.Buffer().ReadAsync().ContinueWith(task => Assert.AreEqual("test", task.Result));
            await pump.Buffer().ReadAsync().ContinueWith(task => Assert.AreEqual("content", task.Result));
        }

        [TestMethod]
        public void From_Should_Throw_If_Pump_Started()
        {
            var source = new Buffer();

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                new Pump()
                    .From(source)
                    .Start()
                    .From(source);
            });
        }

        [TestMethod]
        public void Buffers_Should_Throw_If_Pump_Started()
        {
            var source = new Buffer();

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                new Pump()
                    .From(source)
                    .Start()
                    .Buffers(new Dictionary<String, Buffer> {{"output", source}});
            });
        }

        [TestMethod]
        public void Should_Create_Error_Buffer_On_Start()
        {
            var pump = new Pump()
                .From(new Buffer())
                .Start();

            Assert.IsNotNull(pump.ErrorBuffer);
        }

        [TestMethod]
        public async Task Should_Write_Errors_To_The_Error_Buffer()
        {
            var pump = new Pump()
                .From(new [] {"testData"})
                .Process(async (data, thePump, token) => await Task.FromException(new Exception("derp")))
                .Start();

            await pump.WhenFinished().ContinueWith(task =>
                {
                    Assert.IsFalse(pump.ErrorBuffer.IsEmpty);
                    Assert.AreEqual("derp", ((PumpException)pump.ErrorBuffer.Content.Peek()).Error);
                });
        }

        [TestMethod]
        public async Task Should_Seal_Output_Buffers_When_Source_Buffer_Ends()
        {
            var source = new Buffer();

            var pump = new Pump().From(source);

            var spy = new Spy();
            pump.Buffer().SealedEvent += spy.Action;

            pump.Start();
            source.Seal();

            await pump.WhenFinished().ContinueWith(task =>
            {
                Assert.AreEqual(1, spy.TimesCalled);
            });
        }

        [TestMethod]
        public async Task Should_Emit_EndEvent_When_All_Output_Buffers_Ended()
        {
            var source = new Buffer();

            var pump = new Pump()
                .From(source);

            var spy = new Spy();
            pump.Buffer().EndEvent += spy.Action;

            source.Seal();
            pump.Start();

            await pump.WhenFinished().ContinueWith(task =>
            {
                Assert.AreEqual(1, spy.TimesCalled);
                Assert.IsTrue(pump.IsEnded);
            });
        }

        [TestMethod]
        public async Task Should_Be_Able_To_Transform_The_Data()
        {
            var pump = new Pump()
                .From(new []{"foo", "bar"})
                .Process((data, thePump, ct) => thePump.Buffer().WriteAsync($"{data}!"))
                .Start();

            await pump.Buffer().ReadAsync().ContinueWith(task => Assert.AreEqual("foo!", task.Result));
            await pump.Buffer().ReadAsync().ContinueWith(task => Assert.AreEqual("bar!", task.Result));
        }
    }

    internal class Spy
    {
        public Int32 TimesCalled { get; private set; }
        public Action Action { get; }

        public Spy()
        {
            TimesCalled = 0;
            Action = () => TimesCalled++;
        }
    }
}