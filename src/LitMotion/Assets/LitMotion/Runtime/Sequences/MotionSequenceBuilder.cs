using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using LitMotion.Collections;

namespace LitMotion.Sequences
{
    internal sealed class MotionSequenceBuilderSource : ILinkedPoolNode<MotionSequenceBuilderSource>
    {
        static LinkedPool<MotionSequenceBuilderSource> pool;

        public static MotionSequenceBuilderSource Rent()
        {
            if (!pool.TryPop(out var result)) result = new();
            return result;
        }

        public static void Return(MotionSequenceBuilderSource source)
        {
            if (source.buffer != null)
            {
                ArrayPool<MotionSequenceItem>.Shared.Return(source.buffer);
            }

            source.version++;
            source.buffer = null;
            source.tail = 0;
            source.count = 0;
            source.duration = 0;

            pool.TryPush(source);
        }

        public ref MotionSequenceBuilderSource NextNode => ref next;
        public ushort Version => version;

        MotionSequenceBuilderSource next;
        ushort version;
        MotionSequenceItem[] buffer;
        int count;
        double tail;
        double duration;

        public void Append(MotionHandle handle)
        {
            MotionManager.AddToSequence(ref handle, out var motionDuration);
            AddItem(new MotionSequenceItem(tail, handle));
            tail += motionDuration;
            duration += motionDuration;
        }

        public void Insert(double position, MotionHandle handle)
        {
            MotionManager.AddToSequence(ref handle, out var motionDuration);
            AddItem(new MotionSequenceItem(position, handle));
            duration = Math.Max(duration, position + motionDuration);
        }

        public MotionHandle Run()
        {
            var source = MotionSequenceSource.Rent();
            var handle = LMotion.Create(0.0, duration, (float)duration)
                .WithOnComplete(source.OnCompleteDelegate)
                .WithOnCancel(source.OnCancelDelegate)
                .Bind(source, (x, source) => source.Time = x);

            Array.Sort(buffer, 0, count);
            source.Initialize(handle, buffer.AsSpan(0, count), duration);
            return handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddItem(in MotionSequenceItem item)
        {
            if (buffer == null)
            {
                buffer = ArrayPool<MotionSequenceItem>.Shared.Rent(32);
            }
            else if (buffer.Length == count)
            {
                var newBuffer = ArrayPool<MotionSequenceItem>.Shared.Rent(count * 2);
                ArrayPool<MotionSequenceItem>.Shared.Return(buffer);
                buffer = newBuffer;
            }

            buffer[count] = item;
            count++;
        }
    }

    public struct MotionSequenceBuilder : IDisposable
    {
        internal MotionSequenceBuilderSource source;
        internal ushort version;

        internal MotionSequenceBuilder(MotionSequenceBuilderSource source)
        {
            this.source = source;
            this.version = source.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly MotionSequenceBuilder Append(MotionHandle handle)
        {
            CheckIsDisposed();
            source.Append(handle);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly MotionSequenceBuilder Insert(float position, MotionHandle handle)
        {
            CheckIsDisposed();
            source.Insert(position, handle);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MotionHandle Run()
        {
            CheckIsDisposed();
            var handle = source.Run();
            Dispose();
            return handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            CheckIsDisposed();
            MotionSequenceBuilderSource.Return(source);
            source = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckIsDisposed()
        {
            if (source == null || source.Version != version)
            {
                throw new InvalidOperationException("MotionSequenuceBuilder is either not initialized or has already run.");
            }
        }
    }
}