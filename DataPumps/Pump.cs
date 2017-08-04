using System;
using System.Collections.Generic;
using System.Linq;
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

        public event EventHandler ErrorEvent;
        public event EventHandler EndEvent;

        private const String OutputBufferName = "output";

        private PumpState _state;
        private Buffer _from;
        private String _id;
        private IDictionary<String, Buffer> _buffers;
        private CancellationTokenSource _currentRead;
        private CancellationTokenSource _processing;

        private readonly Boolean _debug;

        public Buffer ErrorBuffer { get; private set; }

        public Pump()
        {
            _state = PumpState.Stopped;
            _from = null;
            _id = null;
            _debug = true;

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

            _from = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _registerFromEndEventHandler();
            return this;
        }

        public Pump From(Pump pump)
        {
            _from = pump?.Buffer() ?? throw new ArgumentNullException(nameof(pump));
            _registerFromEndEventHandler();
            return this;
        }

        public Pump From(IEnumerable<String> enumerable)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

            _from = new Buffer(new BufferOptions
            {
                Content = enumerable,
                Sealed = true
            });
            _registerFromEndEventHandler();
            return this;
        }

        public void WriteError(String error)
        {
            if (ErrorBuffer.IsFull)
            {
                return;
            }

            ErrorBuffer.Write(new PumpException(error, _id));
        }

        public void WriteError(AggregateException ex)
        {
            WriteError(ex?.InnerException?.Message);
        }

        public void SourceEnded()
        {
            _currentRead?.Cancel();
        }

        public IDictionary<String, Buffer> Buffers()
        {
            return _buffers;
        }

        public Pump Buffers(IDictionary<String, Buffer> buffers)
        {
            if (_state == PumpState.Started)
            {
                Abort();
                throw new InvalidOperationException("Cannot change output buffers after pumping has been started");
            }

            _buffers = buffers;
            return this;
        }

        public Buffer Buffer(String name = OutputBufferName)
        {
            if (!_buffers.ContainsKey(name)) throw new ArgumentOutOfRangeException($"No such buffer: {name}");

            return _buffers[name];
        }

        public Pump Buffer(String name, Buffer buffer)
        {
            if (_state == PumpState.Started)
            {
                Abort();
                throw new InvalidOperationException("Cannot change output buffers after pumping has been started");
            }

            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            _buffers[name] = buffer ?? throw new ArgumentNullException(nameof(buffer));
            return this;
        }

        public Pump To(Pump pump, String bufferName)
        {
            pump.From(Buffer(bufferName));
            return this;
        }

        public Pump Start()
        {
            if (_from == null)
            {
                throw new MissingMemberException("Source is not configured");
            }

            if (_state != PumpState.Stopped)
            {
                throw new InvalidOperationException("Pump is already started");
            }

            if (_debug)
            {
                Console.WriteLine($"{DateTime.Now} [{_id ?? "(root)"}] Pump started");
            }

            if (ErrorBuffer == null)
            {
                ErrorBuffer = new Buffer();
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

        private void _registerErrorBufferEvents()
        {
            ErrorBuffer.FullEvent += () =>
            {
                if (_state != PumpState.Started) return;

                Abort();
                ErrorEvent?.Invoke(this, EventArgs.Empty);
            };
        }

        public Pump Abort()
        {
            if (_state == PumpState.Aborted) return this;

            if (_state != PumpState.Started)
            {
                throw new InvalidOperationException("Cannot .Abort() a pump that is not running");
            }
            _state = PumpState.Aborted;
            _processing?.Cancel();
            return this;
        }

        private void _outputBufferEnded()
        {
            if (_buffers.Values.All(buffer => !buffer.IsEnded)) return;

            _state = PumpState.Ended;
            if (_debug)
            {
                Console.WriteLine($"{DateTime.Now} [{_id ?? "(root)"}] Pump ended");
            }
            EndEvent?.Invoke(this, EventArgs.Empty);
        }

        private void _pump()
        {
            if (_from.IsEnded)
            {
                SealOutputBuffers();
                return;
            }

            if (_state == PumpState.Paused || _state == PumpState.Aborted) return;

            _currentRead = new CancellationTokenSource();

            _from.ReadAsync(_currentRead.Token)
                .ContinueWith(ReadContinuation);
        }

        private async Task ReadContinuation(Task<Object> task)
        {
            _currentRead = null;
            if (task.Status == TaskStatus.RanToCompletion)
            {
                await _process(task.Result).ContinueWith(ProcessContinuation);
            }
            else
            {
                _pump();
            }
        }

        private void ProcessContinuation(Task task)
        {
            if (task.IsFaulted)
            {
                WriteError(task.Exception);
            }
            _pump();
        }

        private Func<Object, Pump, CancellationToken, Task> _customProcess;

        private async Task _process(Object data)
        {
            _processing = new CancellationTokenSource();
            if (_customProcess == null)
            {
                await Copy(data);
                return;
            }
            await _customProcess(data, this, _processing.Token);
        }

        public Pump Process(Func<Object, Pump, CancellationToken, Task> process)
        {
            _customProcess = process;
            return this;
        }

        private async Task Copy(Object data)
        {
            await Buffer().WriteAsync(data);
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

        public async Task<Pump> WhenFinished()
        {
            if (IsEnded)
            {
                return await Task.FromResult(this);
            }

            var promise = new TaskCompletionSource<Pump>();
            EndEvent += (sender, args) => promise.SetResult(sender as Pump);
            ErrorEvent += (sender, args) => promise.SetException(new PumpingFailedException());
            return await promise.Task;
        }

        private void _registerFromEndEventHandler()
        {
            _from.EndEvent += SourceEnded;
        }
    }
}