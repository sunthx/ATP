using System;
using System.Collections.Generic;
using ATP.Internal.Models;

namespace ATP.Internal.Services
{
    public interface IAppService
    {
        event Action<CombinationKeys> OnHotKeyReceived;
        List<InstalledProgram> GetAll();
        InstalledProgram Add(string appPath);
        bool SetHotKey(string appId, CombinationKeys key);
        void Dispose();
    }
}