using Solve.Reliability.Rx.Infrastructure.States;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Solve.Reliability.Rx.Infrastructure
{
    /// <summary>
    /// The main circuit breaker class
    /// </summary>
    public class CircuitBreaker
    {

        #region  Threadsafe store for circuit breakers

        private static ConcurrentDictionary<string, CircuitBreaker> CircuitDirectory { get { return _circuitInstancesLazy.Value; } }
        
        private static readonly Lazy<ConcurrentDictionary<string, CircuitBreaker>> _circuitInstancesLazy = new Lazy<ConcurrentDictionary<string, CircuitBreaker>>(() => new ConcurrentDictionary<string, CircuitBreaker>());

        #endregion

        #region Circuit breaker policy

        private CircuitBreakerPolicy _policy = CircuitBreakerPolicy.Default;

        /// <summary>
        /// Surface the policy as read only
        /// </summary>
        public CircuitBreakerPolicy Policy
        {
            get { return _policy; }
        }

        /// <summary>
        /// Apply a policy to the circuit breaker
        /// </summary>
        /// <param name="policy"></param>
        public void ApplyPolicy(CircuitBreakerPolicy policy)
        {
                _policy.CircuitResetTimeout = policy.CircuitResetTimeout;
                _policy.InvocationTimeout = policy.InvocationTimeout;
                _policy.MaxErrors = policy.MaxErrors;
        }

        #endregion

        #region Circuit breaker states

        private ICircuitBreakerState _currentState;
        private ICircuitBreakerState _closed;
        private ICircuitBreakerState _open;
        private ICircuitBreakerState _halfOpen;

        #endregion

        public CircuitBreaker()
        {
            _closed = new CircuitBreakerStateClosed(this);
            _open = new CircuitBreakerStateOpen(this);
            _halfOpen = new CircuitBreakerStateHalfOpen(this);
            _currentState = _closed; //start off closed (aka. all g.)
        }

        /// <summary>
        /// Get a circuit breaker by name - this allows multiple observables that depend on the same datasource to share a circuit breaker
        /// </summary>
        /// <param name="name">Name of the circuit breaker</param>
        /// <returns></returns>
        public static CircuitBreaker GetInstance(string name = "global")
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Please provide the name of the circuit breaker you wish to retreive");
            
            return CircuitDirectory.GetOrAdd(name, (key) => { return new CircuitBreaker(); });
        }

        public void HalfCloseCircuit(ICircuitBreakerState from)
        {
            SwitchState(from, _halfOpen);
        }

        public void CloseCircuit(ICircuitBreakerState from)
        {
            SwitchState(from, _closed);
        }

        public void OpenCircuit(ICircuitBreakerState from)
        {
            SwitchState(from, _open);
        }

        private void SwitchState(ICircuitBreakerState from, ICircuitBreakerState to)
        {
            if (Interlocked.CompareExchange(ref _currentState, to, from) == from)
            {
                to.Enter();
            }
        }

        public void OperationSucceeded()
        {
            _currentState.OperationSucceeded();
        }

        public CircuitBreakerState State
        {
            get
            {
                return _currentState.State;
            }
        }

        public IObservable<T> GetSourceAfterFailure<T>(Exception ex, IObservable<T> source, IObservable<T> alternateSource)
        {
            _currentState.OperationFailed();

            if (_currentState.IsTripped)
            {
                return alternateSource;
            }
            else
            {
                return source;
            }
        }
    }
}
