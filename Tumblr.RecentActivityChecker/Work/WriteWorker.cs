using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tumblr.RecentActivityChecker.Work
{
    internal class WriteWorker : IDisposable
    {
        private bool _disposed;

        private readonly string _fileNameSuffix;
        private readonly SemaphoreSlim _lock;
        private readonly Dictionary<int, HashSet<string>> _users;

        public WriteWorker(string fileNameSuffix)
        {
            _fileNameSuffix = fileNameSuffix;
            _lock = new SemaphoreSlim(1, 1);
            _users = new Dictionary<int, HashSet<string>>();
        }

        public async Task AddUsersAsync(
            IReadOnlyCollection<TumblrContact> users)
        {
            await _lock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                foreach (var user in users)
                {
                    var days = user.TimeSinceLastActivity.Days;
                    if (!_users.ContainsKey(days))
                    {
                        _users.Add(
                            days,
                            new HashSet<string>()
                        );
                    }

                    _users[days].Add(user.ToString());
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void AddUser(TumblrContact user)
        {
            _lock.Wait();

            try
            {
                var days = user.TimeSinceLastActivity.Days;
                if (!_users.ContainsKey(days))
                {
                    _users.Add(
                        days,
                        new HashSet<string>()
                    );
                }

                _users[days].Add(user.ToString());
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AddUserAsync(TumblrContact user)
        {
            await _lock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                var days = user.TimeSinceLastActivity.Days;
                if (!_users.ContainsKey(days))
                {
                    _users.Add(
                        days,
                        new HashSet<string>()
                    );
                }

                _users[days].Add(user.ToString());
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

                    foreach (var key in _users.Keys)
                    {
                        if (_users[key].Count > 900)
                        {
                            await WriteUsers(key, _users[key])
                                .ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { /**/ }

            foreach (var key in _users.Keys)
            {
                await WriteUsers(key, _users[key])
                    .ConfigureAwait(false);
            }
        }

        private async Task WriteUsers(
            int key,
            HashSet<string> users)
        {
            await _lock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                using (var streamWriter = new StreamWriter(
                    $"{key}-{_fileNameSuffix}",
                    true))
                {
                    foreach (var user in users)
                    {
                        await streamWriter.WriteLineAsync(user)
                            .ConfigureAwait(false);
                    }
                }

                users.Clear();
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
