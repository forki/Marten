﻿using System;
using System.Threading.Tasks;

namespace Marten.Events.Projections.Async.ErrorHandling
{
    public interface IDaemonErrorHandler
    {
        Task TryAction(Func<Task> action, IMonitoredActivity activity, int attempts = 0);
    }

    public class DaemonErrorHandler : IDaemonErrorHandler
    {
        private readonly IDaemon _daemon;
        private readonly IDaemonLogger _logger;
        private readonly ExceptionHandling _handling;

        public DaemonErrorHandler(IDaemon daemon, IDaemonLogger logger, ExceptionHandling handling)
        {
            _daemon = daemon;
            _logger = logger;
            _handling = handling;
        }


        public async Task TryAction(Func<Task> action, IMonitoredActivity activity, int attempts = 0)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                var handler = _handling.DetermineAction(ex, activity);
                var continuation = await handler.Handle(ex, attempts + 1, activity);

                switch (continuation)
                {
                    case ExceptionAction.Retry:
                        await TryAction(action, activity, attempts + 1);
                        break;

                    case ExceptionAction.Pause:
                        await pause(activity).ConfigureAwait(false);
                        break;

                    case ExceptionAction.Stop:
                        await stop(activity).ConfigureAwait(false);
                        break;

                    case ExceptionAction.StopAll:
                        await stopAll().ConfigureAwait(false);
                        break;
                }

            }
        }

        private async Task stopAll()
        {
            try
            {
                await _daemon.StopAll().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }

        private async Task stop(IMonitoredActivity activity)
        {
            try
            {
                await activity.Stop().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private async Task pause(IMonitoredActivity activity)
        {
            try
            {
                await activity.Stop().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            await Task.Delay(_handling.Cooldown).ConfigureAwait(false);

            try
            {
                await activity.Start().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
    }
}