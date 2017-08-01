using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataPumps
{

    public class Pump
    {
        private enum PumpState
        {
            Stopped,
            Started,
            Paused,
            Ended,
            Aborted
        }

        private const String OutputBufferName = "output";

        private String _id;
        private PumpState _state;
        private Buffer _from;
        private CancellationTokenSource _currentRead;

        private readonly Buffer _errorBuffer;
        private readonly Boolean _debug;
        private readonly IDictionary<String, Buffer> _buffers;

        public Pump()
        {
            _state = PumpState.Stopped;
            _from = null;
            _id = null;
            _errorBuffer = new Buffer();
            _debug = false;

            _buffers = new Dictionary<String, Buffer>
            {
                {OutputBufferName, new Buffer()}
            };
        }

        public Boolean IsStopped => _state == PumpState.Stopped;
        public Boolean IsStarted => _state == PumpState.Started;
        public Boolean IsPaused => _state == PumpState.Paused;
        public Boolean IsEnded => _state == PumpState.Ended;

        public String Id()
        {
            return _id;
        }

        public Pump Id(String id)
        {
            _id = id;
            return this;
        }

        public Buffer Buffer(String name = OutputBufferName)
        {
            if (!_buffers.ContainsKey(name)) throw new ArgumentOutOfRangeException($"No such buffer: {name}");

            return _buffers[name];
        }

        public Pump AddBuffer(String name, Buffer buffer)
        {
            if (_state == PumpState.Started)
            {
                throw new InvalidOperationException("Cannto change output buffers after pumping has been started");
            }

            _buffers[name] = buffer ?? throw new ArgumentException(nameof(buffer));
            return this;
        }

        public Buffer From()
        {
            return _from;
        }

        public Pump From(Buffer buffer)
        {
            if (_state == PumpState.Started)
            {
                throw new InvalidOperationException("Cannot change source buffer after pumping has started");
            }

            _from = buffer ?? throw new ArgumentException(nameof(buffer));
            _registerFromEndEventHandler();
            return this;
        }

        public Pump From(Pump pump)
        {
            _from = pump?.Buffer() ?? throw new ArgumentException(nameof(pump));
            _registerFromEndEventHandler();
            return this;
        }

        public Pump From(IEnumerable<String> enumerable)
        {
            if (enumerable == null) throw new ArgumentException(nameof(enumerable));

            _from = new Buffer(new BufferOptions
            {
                Content = enumerable,
                Sealed = true
            });
            _registerFromEndEventHandler();
            return this;
        }

        public Pump To(Pump pump, String bufferName)
        {
            pump.From(Buffer(bufferName));
            return this;
        }

        public Pump Process(Func<Object, Task<Buffer>> process)
        {
            _customProcess = process;
            return this;
        }

        public Pump Start()
        {
            if (_from == null)
            {
                throw new Exception("Source is not configured");
            }

            if (_state != PumpState.Stopped)
            {
                throw new Exception("Pump is already started");
            }

            if (_debug)
            {
                Console.WriteLine($"{DateTime.Now} [{_id ?? "(root)"}] Pump started");
            }

            _state = PumpState.Started;
            _registerErrorBufferEvents();

            foreach (var key in _buffers.Keys)
            {
                _buffers[key].EndEvent += _outputBufferEnded;
            }
            _pump();
            return this;
        }

        public Pump Abort()
        {
            if (_state == PumpState.Aborted)
            {
                return this;
            }

            if (_state != PumpState.Started)
            {
                throw new Exception("Cannot .Abort() a pump that is not running");
            }
            _state = PumpState.Aborted;
            return this;
        }

        public Pump WriteError(Object error)
        {
            if (_errorBuffer.IsFull)
            {
                return this;
            }

            // TODO: Errors or something?
            _errorBuffer.Write(_id);
            return this;
        }

        private void _registerErrorBufferEvents()
        {
            _errorBuffer.FullEvent += () =>
            {
                if (_state == PumpState.Started)
                {
                    Abort();
                    // TODO Emit Error Event
                }
            };
        }

        private void _outputBufferEnded()
        {
            throw new NotImplementedException();
        }

        private void _pump()
        {
            if (_from.IsEnded) SealOutputBuffers();

            if (_state == PumpState.Paused || _state == PumpState.Aborted)
            {
                return;
            }

            _currentRead = new CancellationTokenSource();

            _from
                .ReadAsync(_currentRead.Token)
                .ContinueWith(async task =>
                {
                    _currentRead = null;
                    await _process(task.Result);
                    _pump();
                });
        }

        private Func<Object, Task<Buffer>> _customProcess;

        private async Task<Buffer> _process(Object data)
        {
            if (_customProcess == null)
            {
                return await Copy(data);
            }
            return await _customProcess(data);
        }

        private async Task<Buffer> Copy(Object data)
        {
            return await Buffer().WriteAsync(data);
        }

        private void SealOutputBuffers()
        {
            foreach (var buffer in _buffers.Values)
            {
                if (!buffer.IsSealed)
                {
                    buffer.Seal();
                }
            }
        }

        private void SourceEnded()
        {
            _currentRead?.Cancel();
        }

        private void _registerFromEndEventHandler()
        {
            _from.EndEvent += SourceEnded;
        }
    }
}