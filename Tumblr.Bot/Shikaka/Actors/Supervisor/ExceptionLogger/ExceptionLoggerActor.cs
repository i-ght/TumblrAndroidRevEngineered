using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Shikaka.Messages.ExceptionLogger;
using Tumblr.Bot.UserInterface;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.ExceptionLogger
{
    internal class ExceptionLoggerActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        /// <inheritdoc />
        /// <summary>
        /// Creates a new instance of a <see cref="T:Tumblr.Bot.Shikaka.Actors.Supervisor.ExceptionLogger.ExceptionLoggerActor" />
        /// </summary>
        public ExceptionLoggerActor()
        {
            ReceiveAsync<LogExceptionMessage>(
                OnReceivedLogExceptionMessage
            );
        }

        /// <summary>
        /// This method is invoked when the actor recieves a <see cref="LogExceptionMessage"/>.
        /// This message is received from <see cref="SupervisorActor"/>.
        /// The message handler writes the exception to file.
        /// </summary>
        /// <param name="exceptionMessage"></param>
        /// <returns></returns>
        public async Task<bool> OnReceivedLogExceptionMessage(
            LogExceptionMessage exceptionMessage)
        {
            if (!global::Waifu.Sys.Settings.Get<bool>(Constants.LogAllExceptions) &&
                exceptionMessage.Exception is InvalidOperationException)
            {
                return true;
            }

            using (var fileStream = new FileStream(
                "exceptions.txt",
                FileMode.Append,
                FileAccess.Write))
            {
                using (var sw = new StreamWriter(fileStream))
                {
                    var ex = exceptionMessage.Exception;
                    var exLogStr = ExToLogString(ex);
                    await sw.WriteLineAsync(exLogStr)
                        .ConfigureAwait(false);
                }
            }

            return true;
        }

        private static string ExToLogString(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Source: {ex.Source}");
            sb.AppendLine($"Exception type: {ex.GetType().FullName}");
            sb.AppendLine($"Method: {ex.TargetSite.Name}");
            sb.AppendLine($"Date: {DateTime.Now.ToLongDateString()}");
            sb.AppendLine($"Time: {DateTime.Now.ToLongTimeString()}");
            sb.AppendLine($"Error: {ex.Message.Trim()}");
            sb.AppendLine($"Stack trace: {ex.StackTrace}");

            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                sb.AppendLine("Inner Exception~");
                sb.AppendLine($"Source: {innerEx.Source}");
                sb.AppendLine($"Exception type: {innerEx.GetType().FullName}");
                sb.AppendLine($"Method: {innerEx.TargetSite.Name}");
                sb.AppendLine($"Date: {DateTime.Now.ToLongDateString()}");
                sb.AppendLine($"Time: {DateTime.Now.ToLongTimeString()}");
                sb.AppendLine($"Error: {innerEx.Message.Trim()}");
                sb.AppendLine($"Stack trace: {innerEx.StackTrace}");

                innerEx = innerEx.InnerException;
            }

            sb.AppendLine("========================================================================");
            return sb.ToString();

        }

        public static Props CreateProps()
        {
            return Props.Create<ExceptionLoggerActor>();
        }
    }
}
