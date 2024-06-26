using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace LitMotion
{
    /// <summary>
    /// A job that updates the status of the motion data and outputs the current value.
    /// </summary>
    /// <typeparam name="TValue">The type of value to animate</typeparam>
    /// <typeparam name="TOptions">The type of special parameters given to the motion data</typeparam>
    /// <typeparam name="TAdapter">The type of adapter that support value animation</typeparam>
    [BurstCompile]
    public unsafe struct MotionUpdateJob<TValue, TOptions, TAdapter> : IJobParallelFor
        where TValue : unmanaged
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
    {
        [NativeDisableUnsafePtrRestriction] public MotionData<TValue, TOptions>* DataPtr;
        [ReadOnly] public double DeltaTime;
        [ReadOnly] public double UnscaledDeltaTime;
        [ReadOnly] public double RealDeltaTime;

        [WriteOnly] public NativeList<int>.ParallelWriter CompletedIndexList;
        [WriteOnly] public NativeArray<TValue> Output;

        public void Execute([AssumeRange(0, int.MaxValue)] int index)
        {
            var ptr = DataPtr + index;

            if (Hint.Likely(ptr->Status is MotionStatus.Scheduled or MotionStatus.Delayed or MotionStatus.Playing))
            {
                var deltaTime = ptr->TimeKind switch
                {
                    MotionTimeKind.Time => DeltaTime,
                    MotionTimeKind.UnscaledTime => UnscaledDeltaTime,
                    MotionTimeKind.Realtime => RealDeltaTime,
                    _ => default
                };

                ptr->Time = math.max(ptr->Time + deltaTime * ptr->PlaybackSpeed, 0.0);
                var motionTime = ptr->Time;

                double t;
                bool isCompleted;
                bool isDelayed;
                int completedLoops;
                int clampedCompletedLoops;

                if (Hint.Unlikely(ptr->Duration <= 0f))
                {
                    if (ptr->DelayType == DelayType.FirstLoop || ptr->Delay == 0f)
                    {
                        var time = motionTime - ptr->Delay;
                        isCompleted = ptr->Loops >= 0 && time > 0f;
                        if (isCompleted)
                        {
                            t = 1f;
                            completedLoops = ptr->Loops;
                        }
                        else
                        {
                            t = 0f;
                            completedLoops = time < 0f ? -1 : 0;
                        }
                        clampedCompletedLoops = ptr->Loops < 0 ? math.max(0, completedLoops) : math.clamp(completedLoops, 0, ptr->Loops);
                        isDelayed = time < 0;
                    }
                    else
                    {
                        completedLoops = (int)math.floor(motionTime / ptr->Delay);
                        clampedCompletedLoops = ptr->Loops < 0 ? math.max(0, completedLoops) : math.clamp(completedLoops, 0, ptr->Loops);
                        isCompleted = ptr->Loops >= 0 && clampedCompletedLoops > ptr->Loops - 1;
                        isDelayed = !isCompleted;
                        t = isCompleted ? 1f : 0f;
                    }
                }
                else
                {
                    if (ptr->DelayType == DelayType.FirstLoop)
                    {
                        var time = motionTime - ptr->Delay;
                        completedLoops = (int)math.floor(time / ptr->Duration);
                        clampedCompletedLoops = ptr->Loops < 0 ? math.max(0, completedLoops) : math.clamp(completedLoops, 0, ptr->Loops);
                        isCompleted = ptr->Loops >= 0 && clampedCompletedLoops > ptr->Loops - 1;
                        isDelayed = time < 0f;

                        if (isCompleted)
                        {
                            t = 1f;
                        }
                        else
                        {
                            var currentLoopTime = time - ptr->Duration * clampedCompletedLoops;
                            t = math.clamp(currentLoopTime / ptr->Duration, 0f, 1f);
                        }
                    }
                    else
                    {
                        var currentLoopTime = math.fmod(motionTime, ptr->Duration + ptr->Delay) - ptr->Delay;
                        completedLoops = (int)math.floor(motionTime / (ptr->Duration + ptr->Delay));
                        clampedCompletedLoops = ptr->Loops < 0 ? math.max(0, completedLoops) : math.clamp(completedLoops, 0, ptr->Loops);
                        isCompleted = ptr->Loops >= 0 && clampedCompletedLoops > ptr->Loops - 1;
                        isDelayed = currentLoopTime < 0;

                        if (isCompleted)
                        {
                            t = 1f;
                        }
                        else
                        {
                            t = math.clamp(currentLoopTime / ptr->Duration, 0f, 1f);
                        }
                    }
                }

                float progress;
                switch (ptr->LoopType)
                {
                    default:
                    case LoopType.Restart:
                        progress = GetEasedValue(ptr, (float)t);
                        break;
                    case LoopType.Yoyo:
                        progress = GetEasedValue(ptr, (float)t);
                        if ((clampedCompletedLoops + (int)t) % 2 == 1) progress = 1f - progress;
                        break;
                    case LoopType.Incremental:
                        progress = GetEasedValue(ptr, 1f) * clampedCompletedLoops + GetEasedValue(ptr, (float)math.fmod(t, 1f));
                        break;
                }

                var totalDuration = ptr->DelayType == DelayType.FirstLoop
                    ? ptr->Delay + ptr->Duration * ptr->Loops
                    : (ptr->Delay + ptr->Duration) * ptr->Loops;

                if (ptr->Loops > 0 && motionTime >= totalDuration)
                {
                    ptr->Status = MotionStatus.Completed;
                }
                else if (isDelayed)
                {
                    ptr->Status = MotionStatus.Delayed;
                }
                else
                {
                    ptr->Status = MotionStatus.Playing;
                }

                var context = new MotionEvaluationContext()
                {
                    Progress = progress
                };

                Output[index] = default(TAdapter).Evaluate(ref ptr->StartValue, ref ptr->EndValue, ref ptr->Options, context);
            }
            else if (ptr->Status is MotionStatus.Completed or MotionStatus.Canceled)
            {
                CompletedIndexList.AddNoResize(index);
                ptr->Status = MotionStatus.Disposed;
            }
        }

        static float GetEasedValue(MotionData<TValue, TOptions>* data, float value)
        {
            return data->Ease switch
            {
                Ease.CustomAnimationCurve => data->AnimationCurve.Evaluate(value),
                _ => EaseUtility.Evaluate(value, data->Ease)
            };
        }
    }
}