using System;
using System.Threading;
using System.Threading.Tasks;
using Tumblr.Scraper.UserInterface;
using Waifu.Sys;

namespace Tumblr.Scraper.Work
{
    internal abstract class Mode
    {
        protected Mode(
            int index,
            DataGridItem ui)
        {
            Index = index;
            UiDataGridItem = ui;
        }

        protected int Index { get; }
        protected DataGridItem UiDataGridItem { get; }

        public abstract Task BaseAsync();

        protected void UpdateThreadStatus(
            string s,
            int delay)
        {
            UiDataGridItem.Status = s;
            Thread.Sleep(delay);
        }

        protected async Task UpdateThreadStatusAsync(
            string s,
            TimeSpan delay)
        {
            await UpdateThreadStatusAsync(s, delay, CancellationToken.None)
                .ConfigureAwait(false);
        }

        protected async Task UpdateThreadStatusAsync(
            string s,
            TimeSpan delay,
            CancellationToken c)
        {
            UiDataGridItem.Status = s;
            await Task.Delay(delay, c)
                .ConfigureAwait(false);
        }

        protected virtual async Task LogExceptionAndUpdateUserInterface(
            Exception e)
        {
            await UpdateThreadStatusAsync(
                $"{e.GetType().Name} occured ~ {e.Message}",
                TimeSpan.FromSeconds(5)
            ).ConfigureAwait(false);

            await LogException(e)
                .ConfigureAwait(false);
        }

        protected async Task LogException(
            Exception e)
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
