# Core pooling

Import this sample from the Core package, open `CorePooling.unity`, then enter Play. The component logs `TPLab Core pooling sample passed.` after checking rent, capacity, return reset, reuse, loan counts and final owner cleanup. Exceptions fail the sample. The component owns and disposes the pool within one synchronous run.

`CorePoolingSample.Run()` also runs the same checks directly from a consumer smoke runner. This sample references only `TPLab.Core`; it uses no Input System or uGUI.
