using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Vostok.ZooKeeper.LocalEnsemble
{
    internal interface IWindowsProcessKillJob : IDisposable
    {
        void AddProcess([NotNull] Process process);

        void AddProcess(IntPtr processHandle);
    }
}