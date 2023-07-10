using System;
using System.Threading;
using System.Threading.Tasks;
using Waifu.Sys;

namespace Tumblr.Creator
{
    internal abstract class Mode
    {
        protected Mode(int index, DataGridItem ui)
        {
            Index = index;
            UI = ui;
        }

        protected int Index { get; }
        protected DataGridItem UI { get; }

        public abstract Task BaseAsync();

        protected void UpdateThreadStatus(string s, int delay)
        {
            UI.Status = s;
            Thread.Sleep(delay);
        }

        protected async Task UpdateThreadStatusAsync(string s, int delay)
        {
            await UpdateThreadStatusAsync(s, delay, CancellationToken.None)
                .ConfigureAwait(false);
        }

        protected async Task UpdateThreadStatusAsync(
            string s,
            int delay,
            CancellationToken c)
        {
            UI.Status = s;
            await Task.Delay(delay, c)
                .ConfigureAwait(false);
        }

        protected virtual async Task LogExceptionAndUpdateUI(Exception e)
        {
            await UpdateThreadStatusAsync(
                $"{e.GetType().Name} occured ~ {e.Message}",
                5000
            ).ConfigureAwait(false);

            await LogException(e)
                .ConfigureAwait(false);
        }

        protected async Task LogException(Exception e)
        {
            if (Settings.Get<bool>(Constants.LogAllExceptions))
            {
                await ErrorLogger.WriteAsync(e, false)
                    .ConfigureAwait(false);
                return;
            }

            switch (e)
            {
                case InvalidOperationException _:
                case TimeoutException _:
                case OperationCanceledException _:
                    break;

                default:
                    await ErrorLogger.WriteAsync(e)
                        .ConfigureAwait(false);
                    break;
            }
        }
    }
}
