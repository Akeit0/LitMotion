# Motion Configuration

You can add settings such as easing, repeat count, callbacks, etc., when creating a motion. To add settings, use the `With-` methods.

When writing, you can apply multiple settings simultaneously using method chaining.

```cs
var value = 0f;
LMotion.Create(0f, 10f, 2f)
    .WithEase(Ease.OutQuad)
    .WithComplete(() => Debug.Log("Complete!"))
    .Bind(x => value = x);
```

### List of Methods

#### WithEase

Specifies the easing function to apply to the motion.

#### WithDelay

Delay the start of a motion by a specified number of seconds. You can adjust the behavior by specifying `DelayType` and `SkipValuesDuringDelay`.

* DelayType

    Specifies the behavior of delay during looping.

    | DelayType           | Behavior                                               |
    | ------------------- | ------------------------------------------------------ |
    | DelayType.FirstLoop | Default setting. Applies delay only in the first loop. |
    | DelayType.EveryLoop | Applies delay in each loop.                            |

* SkipValuesDuringDelay

  Specifies whether to skip the processing of `Bind` during the delay time. It is set to `true` by default.

#### WithLoops

Sets the number of times the motion will repeat. It's set to 1 by default.
Setting it to -1 creates a motion that repeats infinitely until stopped.

Additionally, you can set the behavior during repetition by specifying `LoopType` as the second argument.

| LoopType | Behavior |
| - | - |
| LoopType.Restart | Default behavior. Resets to the start value at the end of each loop. |
| LoopType.Yoyo | Animates the value back and forth between start and end values. |
| LoopType.Increment | Value increases with each loop. |

#### WithOnComplete

Specifies a callback at the end of the playback.

#### WithOnCancel

Specifies a callback for when the motion is canceled.

#### WithScheduler

Specifies the Scheduler used for motion playback.

| Scheduler | Behavior |
| - | - |
| MotionScheduler.Initialization | Updates at the Initialization timing. |
| MotionScheduler.InitializationIgnoreTimeScale | Updates at the Initialization timing, ignores the influence of `Time.timeScale`. |
| MotionScheduler.InitializationRealtime | Updates at the Initialization timing, ignores the influence of `Time.timeScale`, and calculates time using `Time.realtimeSinceStartup`. |
| MotionScheduler.EarlyUpdate | Updates at the EarlyUpdate timing. |
| MotionScheduler.EarlyUpdateIgnoreTimeScale | Updates at the EarlyUpdate timing, ignores the influence of `Time.timeScale`. |
| MotionScheduler.EarlyUpdateRealtime | Updates at the EarlyUpdate timing, ignores the influence of `Time.timeScale`, and calculates time using `Time.realtimeSinceStartup`. |
| MotionScheduler.FixedUpdate | Updates at the FixedUpdate timing. |
| MotionScheduler.PreUpdate | Updates at the PreUpdate timing. |
| MotionScheduler.PreUpdateIgnoreTimeScale | Updates at the PreUpdate timing, ignores the influence of `Time.timeScale`. |
| MotionScheduler.PreUpdateRealtime | Updates at the PreUpdate timing, ignores the influence of `Time.timeScale`, and calculates time using `Time.realtimeSinceStartup`. |
| MotionScheduler.Update | Updates at the Update timing. |
| MotionScheduler.UpdateIgnoreTimeScale | Updates at the Update timing, ignores the influence of `Time.timeScale`. |
| MotionScheduler.UpdateRealtime | Updates at the Update timing, ignores the influence of `Time.timeScale`, and calculates time using `Time.realtimeSinceStartup`. |
| MotionScheduler.PreLateUpdate | Updates at the PreLateUpdate timing. |
| MotionScheduler.PreLateUpdateIgnoreTimeScale | Updates at the PreLateUpdate timing, ignores the influence of `Time.timeScale`. |
| MotionScheduler.PreLateUpdateRealtime | Updates at the PreLateUpdate timing, ignores the influence of `Time.timeScale`, and calculates time using `Time.realtimeSinceStartup`. |
| MotionScheduler.PostLateUpdate | Updates at the PostLateUpdate timing. |
| MotionScheduler.PostLateUpdateIgnoreTimeScale | Updates at the PostLateUpdate timing, ignores the influence of `Time.timeScale`. |
| MotionScheduler.PostLateUpdateRealtime | Updates at the PostLateUpdate timing, ignores the influence of `Time.timeScale`, and calculates time using `Time.realtimeSinceStartup`. |
| MotionScheduler.TimeUpdate | Updates at the TimeUpdate timing. |
| MotionScheduler.TimeUpdateIgnoreTimeScale | Updates at the TimeUpdate timing, ignores the influence of `Time.timeScale`. |
| MotionScheduler.TimeUpdateRealtime | Updates at the TimeUpdate timing, ignores the influence of `Time.timeScale`, and calculates time using `Time.realtimeSinceStartup`. |
| MotionScheduler.Manual | Updates manually. For details, see [Updating Motion Manually](updating-motion-manually.md).  |
| EditorMotionScheduler.Update (LitMotion.Editor) | Updates at the EditorApplication.update timing. This Scheduler is limited to the editor. |

#### WithRoundingMode (int)

Sets the rounding mode for decimal values. This option is only applicable to int-type motions.

| RoundingMode | Behavior |
| - | - |
| RoundingMode.ToEven | Default setting. Rounds the value to the nearest integer and, in case of a tie, rounds to the nearest even number. |
| RoundingMode.AwayFromZero | Typical rounding behavior. Rounds the value to the nearest integer and away from zero in case of a tie. |
| RoundingMode.ToZero | Rounds the value towards zero. |
| RoundingMode.ToPositiveInfinity | Rounds the value towards positive infinity. |
| RoundingMode.ToNegativeInfinity | Rounds the value towards negative infinity. |

#### WithScrambleMode (FixedString-)

You can fill the yet-to-be-displayed characters with random characters. This option is applicable only to string motions.

| ScrambleMode | Description |
| - | - |
| ScrambleMode.None | Default setting. Nothing is displayed in the yet-to-be-displayed parts. |
| ScrambleMode.Uppercase | Fills spaces with random uppercase alphabet characters. |
| ScrambleMode.Lowercase | Fills spaces with random lowercase alphabet characters. |
| ScrambleMode.Numerals | Fills spaces with random numeral characters. |
| ScrambleMode.All | Fills spaces with random uppercase/lowercase alphabet or numeral characters. |
| (ScrambleMode.Custom) | Fills spaces with random numeral characters from the specified string. This option cannot be explicitly specified and is set when passing a string argument to WithScrambleMode. |

#### WithRichText (FixedString-)

Enables RichText support, allowing character advancement in text containing RichText tags. This option is applicable only to string motions. 

#### WithFrequency (Punch, Shake)

Sets the frequency (number of oscillations until the end) for Punch and Shake vibrations. The default value is set to 10.

#### WithDampingRatio (Punch, Shake)

Sets the damping ratio for Punch and Shake vibrations. When this value is 1, it fully dampens, and when it's 0, there is no damping at all. The default value is set to 1.

#### WithRandomSeed (FixedString-, Shake)

Allows you to specify a random seed used during motion playback. This controls the random behavior of ScrambleChars or vibrations. 