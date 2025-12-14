using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Majinfwork.TaskHelper {
    public static class RetryTask {
        public static async Task<bool> CountLoop(int retries, int delay, Func<CancellationToken, Task<bool>> func, CancellationToken cancel) {
            while (retries > 0) {
                retries--;
                if (await func(cancel)) {
                    return true;
                }
                await Task.Delay(delay, cancel);
            }
            return false;
        }

        public static async Task<bool> TimeoutLoop(float timeout, int delay, Func<CancellationToken, Task<bool>> func, CancellationToken cancel) {
            var time = Time.unscaledTime;
            while (Time.unscaledTime - time <= timeout) {
                if (await func(cancel)) {
                    return true;
                }
                await Task.Delay(delay, cancel);
            }
            return false;
        }

        public static async Task<T> TimeoutLoop<T>(float timeout, int delay, Func<CancellationToken, Task<T>> func, Func<T, bool> isSuccessfulFunc, CancellationToken cancel) {
            var time = Time.unscaledTime;
            T result = default;
            while (Time.unscaledTime - time <= timeout) {
                result = await func(cancel);
                if (isSuccessfulFunc(result)) {
                    return result;
                }
                await Task.Delay(delay, cancel);
            }
            return result;
        }

        public static async Task<int> FailedTasksCount(List<Task<bool>> tasks) {
            var failedCount = 0;
            foreach (var task in tasks) {
                if (!await task)
                    failedCount++;
            }
            return failedCount;
        }
    }
}