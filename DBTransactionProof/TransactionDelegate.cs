using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Transactions;

namespace DBTransactionProof
{
    public class TransactionDelegate : ITransactionDelegate
    {
        private ILogger<TransactionDelegate> _logger;
        public TransactionDelegate(ILogger<TransactionDelegate> logger)
        {
            _logger = logger;
        }

        public async Task DoWorkAsync(Func<Task> resultBody)
        {
            using (var transaction = GetTransactionScope())
            {
                try
                {
                    await resultBody();
                    transaction.Complete();
                    _logger.Log(LogLevel.Information, "Complete");
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Information, "Rolling Back");
                    _logger.LogError(ex.Message);
                }
                finally
                {
                    _logger.Log(LogLevel.Information, "DONEEEEE");
                }

            }
        }

        protected TransactionScope GetTransactionScope()
        {
            return new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}
