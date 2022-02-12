using System;
using ATP.Internal.Models;

namespace ATP.Internal.Services
{
    public interface IKeyboardService
    {
        event Action<CombinationKeys> OnReceived;
        void Start();
        void Stop();
    }
}