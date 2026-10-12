using System;
using System.Reflection;
using NUnit.Framework;

namespace TPLab.Core.Tests
{
    public static class TryReturnTestApi
    {
        public static Func<T, bool> Bind<T>(object pool)
        {
            var method = pool.GetType().GetMethod("TryReturn", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(T) }, null);
            Assert.That(method, Is.Not.Null,
                "Required public API: bool " + pool.GetType().Name + ".TryReturn(" + typeof(T).Name + ")");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(bool)));
            // Bind once so calls preserve the original exceptions and allocation measurements exclude reflection.
            return (Func<T, bool>)Delegate.CreateDelegate(typeof(Func<T, bool>), pool, method);
        }
    }
}
