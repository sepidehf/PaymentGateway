using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BankGateway.Domain.Helpers
{
    /// <summary>
    /// Provides functionality to automatically try the given piece of logic some number of times before re-throwing the exception. 
    /// This is useful for any piece of code which may experience transient failures. Be cautious of passing code with two distinct 
    /// actions given that if the second or subsequent piece of logic fails, the first will also be retried upon each retry. 
    /// </summary>
    public static class RetryHelper
    {
        public static TResult RetryOperation<TResult>(Func<TResult> action, int numRetries, int retryTimeout)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            do
            {
                try
                {
                    return action();
                }
                catch
                {
                    if (numRetries <= 0) throw;
                    else
                        Thread.Sleep(retryTimeout);
                }
            } while (numRetries-- > 0);

            return default(TResult);
        }


        public static async Task RetryOnExceptionAsync(int times, TimeSpan delay, Func<Task> operation)
        {
            await RetryOnExceptionAsync<Exception>(times, delay, operation);
        }
                
        public static async Task RetryOnExceptionAsync<TException>( int times, TimeSpan delay, Func<Task> operation) where TException : Exception
        {
            if (times <= 0)
                throw new ArgumentOutOfRangeException(nameof(times));

            var attempts = 0;
            do
            {
                try
                {
                    attempts++;
                    await operation();
                    break;
                }
                catch (TException ex)
                {
                    if (attempts == times)
                        throw;

                    await CreateDelayForException(times, attempts, delay, ex);
                }
            } while (true);
        }

        



        /// <summary>Executes asynchronous function with retry logic.</summary>
        /// <param name="func">The asynchronous function to be executed.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <param name="retryInterval">Timespan to wait between attempts of the operation</param>
        /// <param name="onAttemptFailed">The callback executed when an attempt is failed.</param>
        /// <typeparam name="T">The result type.</typeparam>
        /// <returns>The <see cref="Task"/> producing the result.</returns>
        public static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> func, int maxAttempts, TimeSpan? retryInterval = null, Action<int, Exception> onAttemptFailed = null)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "The maximum number of attempts must not be less than 1.");
            }

            var attempt = 0;

            while (true)
            {
                if (attempt > 0 && retryInterval != null)
                {
                    await Task.Delay(retryInterval.Value);
                }

                try
                {
                    //Call the function passed in by the caller. 
                    return await func().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    attempt++;

                    if (onAttemptFailed != null)
                    {
                        onAttemptFailed(attempt, exception);
                    }

                    if (attempt >= maxAttempts)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>Executes asynchronous function with retry logic.</summary>
        /// <param name="func">The asynchronous function to be executed.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <param name="retryInterval">Timespan to wait between attempts of the operation</param>
        /// <param name="onAttemptFailed">The retry handler.</param>
        /// <returns>The <see cref="Task"/> producing the result.</returns>
        public static async Task ExecuteWithRetry(Func<Task> func, int maxAttempts, TimeSpan? retryInterval = null, Action<int, Exception> onAttemptFailed = null)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            Func<Task<bool>> wrapper = async () =>
            {
                await func().ConfigureAwait(false);
                return true;
            };

            await ExecuteWithRetry(wrapper, maxAttempts, retryInterval, onAttemptFailed);
        }


        private static Task CreateDelayForException(int times, int attempts, TimeSpan delayOfTimeSpan, Exception ex)
        {
            var delay = IncreasingDelayInSeconds(attempts);
            return Task.Delay(delay);
        }

        internal static int[] DelayPerAttemptInSeconds =
        {
            (int) TimeSpan.FromSeconds(2).TotalSeconds,
            (int) TimeSpan.FromSeconds(30).TotalSeconds,
            (int) TimeSpan.FromMinutes(2).TotalSeconds,
            (int) TimeSpan.FromMinutes(10).TotalSeconds,
            (int) TimeSpan.FromMinutes(30).TotalSeconds
        };

        private static int IncreasingDelayInSeconds(int failedAttempts)
        {
            if (failedAttempts <= 0) throw new ArgumentOutOfRangeException();

            return failedAttempts > DelayPerAttemptInSeconds.Length ? DelayPerAttemptInSeconds.Last() : DelayPerAttemptInSeconds[failedAttempts];
        }
    }

}