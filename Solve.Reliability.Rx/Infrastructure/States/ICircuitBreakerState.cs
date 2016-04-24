using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solve.Reliability.Rx.Infrastructure.States
{
    public enum CircuitBreakerState { Open, HalfOpen, Closed }

    public interface ICircuitBreakerState
    {
        void Enter();
        void OperationSucceeded();
        void OperationFailed();

        CircuitBreakerState State { get; }

        bool IsTripped { get; }

    }
}
