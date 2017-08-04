using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataPumps
{
    public delegate void WriteEventHandler(Object data);

    public delegate void ReleaseEventHandler(Object data);

    public delegate void FullEventHandler();

    public delegate void EndEventHandler();

    public delegate void EmptyEventHandler();

    public delegate void SealedEventHandler();

    public class Buffer
    {
        public Queue<Object> Content { get; }
        public Int32 Size { get; }

        private Int32 _defaultSize = 10;

        public event Action<Object> WriteEvent;
        public event Action<Object> ReleaseEvent;
        public event Action FullEvent;
        public event Action EndEvent;
        public event Action EmptyEvent;
        public event Action SealedEvent;

        public Buffer(BufferOptions options = null)
        {

            Content = new Queue<Object>(options?.Content ?? new Object[0]);
            Size = options?.Size ?? _defaultSize;
            IsSealed = options?.Sealed ?? false;
        }

        public Boolean IsEmpty => !Content.Any();

        public Boolean IsFull => Content.Count >= Size;

        public Boolean IsSealed { get; private set; }

        public Boolean IsEnded => IsSealed && IsEmpty;

        public Buffer Write(Object data)
        {
            if (IsSealed)
            {
                throw new InvalidOperationException("Cannot write to sealed buffer");
            }

            if (IsFull)
            {
                throw new InvalidOperationException("Buffer is full");
            }

            Content.Enqueue(data);

            WriteEvent?.Invoke(data);
            if (IsFull)
            {
                FullEvent?.Invoke();
            }
            return this;
        }

        public async Task<Buffer> WriteAsync(Object data, CancellationToken? cancellationToken = null)
        {
            if (!IsFull)
            {
                return await Task.FromResult(Write(data));
            }

            var promise = new TaskCompletionSource<Buffer>();

            cancellationToken?.Register(() =>
            {
                promise.TrySetCanceled();
            });

            ReleaseEvent += async _ =>
            {
                promise.SetResult(await WriteAsync(data, cancellationToken));
            };

            return await promise.Task;
        }

        public Object Read()
        {
            if (IsEmpty) throw new InvalidOperationException("Buffer is empty");

            var result = Content.Dequeue();
            ReleaseEvent?.Invoke(result);

            if (!IsEmpty) return result;

            EmptyEvent?.Invoke();
            if (IsSealed)
            {
                EndEvent?.Invoke();
            }

            return result;
        }

        public async Task<Object> ReadAsync(CancellationToken? cancellationToken = null)
        {
            if (!IsEmpty)
            {
                return await Task.FromResult(Read());
            }

            var promise = new TaskCompletionSource<Object>();

            cancellationToken?.Register(() =>
            {
                promise.TrySetCanceled();
            });

            async void Handler(Object _)
            {
                promise.SetResult(await ReadAsync(cancellationToken));
                WriteEvent -= Handler;
            }

            WriteEvent += Handler;

            return await promise.Task;
        }

        public Buffer Seal()
        {
            if (IsSealed) throw new Exception("Buffer already sealed");

            IsSealed = true;
            SealedEvent?.Invoke();
            if (IsEmpty)
            {
                EndEvent?.Invoke();
            }
            return this;
        }
    }
}