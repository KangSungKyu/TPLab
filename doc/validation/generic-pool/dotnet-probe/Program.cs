using System;
using MyLab.Core.Pooling;

internal static class Program
{
    private static int _checks;

    private sealed class Message : IDisposable
    {
        public int Value;
        public bool IsDisposed;
        public void Dispose() => IsDisposed = true;
    }

    private static void Main()
    {
        var pool = new ObjectPool<Message>(() => new Message(), 1,
            onReturn: item => item.Value = 0, onDestroy: item => item.Dispose());
        Check(pool.TryRent(out var first));
        first.Value = 42;
        pool.Return(first);
        Check(pool.TryRent(out var second) && ReferenceEquals(first, second) && second.Value == 0);
        Check(!pool.TryRent(out var exhausted) && exhausted == null);
        Expect<ArgumentException>(() => pool.Return(new Message()));
        pool.Dispose();
        Check(first.IsDisposed && pool.IsDisposed && pool.CountOwned == 0);
        Expect<ObjectDisposedException>(() => pool.TryRent(out _));
        Console.WriteLine("PASS: " + _checks + " checks; generic source compiled without Unity references.");
    }

    private static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Check failed");
        _checks++;
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { _checks++; return; }
        throw new InvalidOperationException("Expected " + typeof(TException).Name);
    }
}
