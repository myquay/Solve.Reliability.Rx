using Solve.Reliability.Rx.Infrastructure;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Solve.Reliability.Rx
{
    public static class ReactiveCircuitBreakerExtension
    {
        public static IObservable<T> RecoverWith<T>(this IObservable<T> source, IObservable<T> alternateSource, string circuitBreakerName = "global")
        {
            var cb = CircuitBreaker.GetInstance(circuitBreakerName);

            return Observable.Create<T>(observer =>
            {
                source
                    .SubscribeOn(Scheduler.Default) //Do the retry things on a different thread
                    .Subscribe(observer.OnNext, observer.OnError, () => { cb.OperationSucceeded();  observer.OnCompleted(); });
                return Disposable.Empty;
            })
            .Timeout(cb.Policy.InvocationTimeout)
            .Catch((Exception ex) =>
            {
                return cb.GetSourceAfterFailure(ex, source.RecoverWith(alternateSource, circuitBreakerName), alternateSource);
            });
        }
    }
}
