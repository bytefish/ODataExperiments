using Microsoft.EntityFrameworkCore.Diagnostics;
using ODataFga.Services;
using System.Data.Common;

namespace ODataFga.Database
{
    public class RlsInterceptor : DbCommandInterceptor
    {
        private readonly ICurrentUserService _user;
        public RlsInterceptor(ICurrentUserService user) => _user = user;

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            SetRlsVariables(command); return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            SetRlsVariables(command); return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            SetRlsVariables(command); return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            SetRlsVariables(command); return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void SetRlsVariables(DbCommand command)
        {
            var userId = _user.UserId?.Replace("'", "''");
            var mask = _user.RequiredMask;

            string rlsSetup = string.IsNullOrEmpty(userId)
                ? "SET app.current_user = ''; SET app.required_mask = ''; "
                : $"SET app.current_user = '{userId}'; SET app.required_mask = '{mask}'; ";

            // Inject variables into the Postgres connection session right before the query hits
            if (!command.CommandText.StartsWith("SET app."))
            {
                command.CommandText = rlsSetup + command.CommandText;
            }
        }
    }

}
