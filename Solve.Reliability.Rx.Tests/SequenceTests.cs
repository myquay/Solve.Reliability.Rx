using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using Solve.Reliability.Rx.Infrastructure;
using Solve.Reliability.Rx.Infrastructure.States;

namespace Solve.Reliability.Rx.Tests
{
    [TestClass]
    public class SequenceTests
    {
        private IObservable<int> GetSuccessfulObservable()
        {
            return Observable.Create<int>(observer =>
            {
                return Scheduler.Schedule(Scheduler.Default, () =>
                {
                    observer.OnNext(1);
                    observer.OnNext(1);
                    observer.OnCompleted();
                });
            });
        }

        private IObservable<int> GetFailingObservable()
        {
            return Observable.Create<int>(observer =>
            {
                return Scheduler.Schedule(Scheduler.Default, () =>
                {
                    observer.OnNext(1);
                    observer.OnError(new Exception("Failed!"));
                });
            });
        }

        private IObservable<int> GetSuccessfulObservableWithDelay(TimeSpan delay)
        {
            return Observable.Create<int>(observer =>
            {
                return Scheduler.Schedule(Scheduler.Default, () =>
                {
                    observer.OnNext(1);
                    Thread.Sleep((int)delay.TotalMilliseconds);
                    observer.OnNext(1);
                    observer.OnCompleted();
                });
            });
        }

        private IObservable<int> GetAlternateObservable()
        {
            return Observable.Create<int>(observer =>
            {
                return Scheduler.Schedule(Scheduler.Default, () =>
                {
                    observer.OnNext(0);
                    observer.OnNext(0);
                    observer.OnCompleted();
                });
            });
        }

        [TestMethod]
        public void OperationsSucceed()
        {
            var cbName = "OperationsSucceed";


            Assert.IsTrue(CircuitBreaker
                .GetInstance(cbName).State == CircuitBreakerState.Closed, "Circuit breaker should be closed");

            var resultingSequence = GetSuccessfulObservable()
                .RecoverWith(GetAlternateObservable(), cbName)
                .ToEnumerable();

            Assert.IsTrue(resultingSequence.All(x => x == 1), "Only the primarty sequence should run");
            Assert.IsTrue(resultingSequence.Count() == 2, "The primary sequence should run through to end only once");

            Assert.IsTrue(CircuitBreaker
                .GetInstance(cbName).State == CircuitBreakerState.Closed, "Circuit breaker should be closed");
        }

        [TestMethod]
        public void CircuitBreakerClosedToOpen()
        {
            var cbName = "CircuitBreakerClosedToOpen";
            var cb = CircuitBreaker.GetInstance(cbName);

            Assert.IsTrue(cb.State == CircuitBreakerState.Closed, "Circuit breaker should be closed");

            var enumberableSequence = GetFailingObservable()
                .RecoverWith(GetAlternateObservable(), cbName)
                .ToEnumerable();

            var array = enumberableSequence.ToArray();

            Assert.IsTrue(array.Where(x => x == 1).Count() == cb.Policy.MaxErrors, "Circuit breaker should retry by resubscribing to the original stream");
            Assert.IsTrue(array.Where(x => x == 0).Count() == 2, "The alternative sequence should run to the end");
            Assert.IsTrue(cb.State == CircuitBreakerState.Open, "Circuit breaker should be open");
        }

        [TestMethod]
        public void CircuitBreakerClosedToOpenToHalfOpen()
        {
            var cbName = "CircuitBreakerClosedToOpenToHalfOpen";
            var cb = CircuitBreaker.GetInstance(cbName);

            cb.ApplyPolicy(new CircuitBreakerPolicy
                {
                    CircuitResetTimeout = TimeSpan.FromMilliseconds(100),
                    InvocationTimeout = TimeSpan.FromSeconds(1),
                    MaxErrors = 3,
                    RequiredSuccessfulCallsToClose = 1
                });

            Assert.IsTrue(cb.State == CircuitBreakerState.Closed, "Circuit breaker should be closed");

            var resultingSequence = GetFailingObservable()
                .RecoverWith(GetAlternateObservable(), cbName)
                .ToEnumerable()
                .ToArray();

            Assert.IsTrue(resultingSequence.Where(x => x == 1).Count() == cb.Policy.MaxErrors, "Circuit breaker should retry by resubscribing to the original stream");
            Assert.IsTrue(resultingSequence.Where(x => x == 0).Count() == 2, "The alternative sequence should run to the end");
            Assert.IsTrue(cb.State == CircuitBreakerState.Open, cbName);
            Thread.Sleep(110);
            Assert.IsTrue(cb.State == CircuitBreakerState.HalfOpen, cbName);
        }

        [TestMethod]
        public void CircuitBreakerClosedToOpenToHalfOpenToOpen()
        {
            var cbName = "CircuitBreakerClosedToOpenToHalfOpenToOpen";
            var cb = CircuitBreaker.GetInstance(cbName);

            cb.ApplyPolicy(new CircuitBreakerPolicy
            {
                CircuitResetTimeout = TimeSpan.FromMilliseconds(100),
                InvocationTimeout = TimeSpan.FromMilliseconds(100),
                MaxErrors = 3,
                RequiredSuccessfulCallsToClose = 1
            });

            Assert.IsTrue(cb.State == CircuitBreakerState.Closed, "Circuit breaker should be closed");

            var resultingSequence = GetFailingObservable()
                .RecoverWith(GetAlternateObservable(), cbName)
                .ToEnumerable()
                .ToArray();

            Assert.IsTrue(resultingSequence.Where(x => x == 1).Count() == cb.Policy.MaxErrors, "Circuit breaker should retry by resubscribing to the original stream");
            Assert.IsTrue(resultingSequence.Where(x => x == 0).Count() == 2, "The alternative sequence should run to the end");
            Assert.IsTrue(cb.State == CircuitBreakerState.Open, cbName);
            Thread.Sleep(110);
            Assert.IsTrue(cb.State == CircuitBreakerState.HalfOpen, cbName);

            GetSuccessfulObservableWithDelay(TimeSpan.FromMilliseconds(110))
                .RecoverWith(GetAlternateObservable(), cbName)
                .ToEnumerable()
                .ToArray();

            Assert.IsTrue(cb.State == CircuitBreakerState.Open, cbName);
        }

        [TestMethod]
        public void CircuitBreakerClosedToOpenToHalfOpenToClosed()
        {
            var cbName = "CircuitBreakerClosedToOpenToHalfOpenToClosed";
            var cb = CircuitBreaker.GetInstance(cbName);

            cb.ApplyPolicy(new CircuitBreakerPolicy
            {
                CircuitResetTimeout = TimeSpan.FromMilliseconds(100),
                InvocationTimeout = TimeSpan.FromMilliseconds(100),
                MaxErrors = 3,
                RequiredSuccessfulCallsToClose = 1
            });

            Assert.IsTrue(cb.State == CircuitBreakerState.Closed, "Circuit breaker should be closed");

            var resultingSequence = GetFailingObservable()
                .RecoverWith(GetAlternateObservable(), cbName)
                .ToEnumerable()
                .ToArray();

            Assert.IsTrue(resultingSequence.Where(x => x == 1).Count() == cb.Policy.MaxErrors, "Circuit breaker should retry by resubscribing to the original stream");
            Assert.IsTrue(resultingSequence.Where(x => x == 0).Count() == 2, "The alternative sequence should run to the end");
            Assert.IsTrue(cb.State == CircuitBreakerState.Open, cbName);
            Thread.Sleep(110);
            Assert.IsTrue(cb.State == CircuitBreakerState.HalfOpen, cbName);

            GetSuccessfulObservableWithDelay(TimeSpan.FromMilliseconds(90))
                .RecoverWith(GetAlternateObservable(), cbName)
                .ToEnumerable()
                .ToArray();

            Assert.IsTrue(cb.State == CircuitBreakerState.Closed, cbName);
        }
    }
}
