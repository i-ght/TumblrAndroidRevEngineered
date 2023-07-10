using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tumblr.Scraper.Work
{
    internal class WriteWorker : IDisposable
    {
        private bool _disposed;

        private readonly SemaphoreSlim _lock;
        private readonly HashSet<string> _users;
        private readonly string _outputFilename;

        public WriteWorker(string outputFileName)
        {
            _outputFilename = outputFileName;
            _lock = new SemaphoreSlim(1, 1);
            _users = new HashSet<string>();
        }

        public async Task AddUsersAsync(
            IReadOnlyCollection<string> users)
        {
            await _lock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                foreach (var user in users)
                    _users.Add(user);
            }
            finally
            {
                _lock.Release();
            }
        }

        public void AddUser(string user)
        {
            _lock.Wait();

            try
            {
                _users.Add(user);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AddUserAsync(string user)
        {
            await _lock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                _users.Add(user);
            }
            finally
            {
                _lock.Release();
            }
        }


        public async Task Base(CancellationToken c)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    await Task.Delay(20000, c)
                        .ConfigureAwait(false);

                    if (_users.Count < 900)
                        continue;

                    await WriteUsers()
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /**/ }


            await WriteUsers()
                .ConfigureAwait(false);
        }

        private async Task WriteUsers()
        {
            await _lock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                using (var streamWriter = new StreamWriter(
                    _outputFilename, true))
                {
                    foreach (var user in _users)
                    {
                        await streamWriter.WriteLineAsync(user)
                            .ConfigureAwait(false);
                    }
                }

                _users.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _users.Clear();
                _lock.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
