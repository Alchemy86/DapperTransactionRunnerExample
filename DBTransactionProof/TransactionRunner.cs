using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace DBTransactionProof
{
    public class TransactionRunner : IHostedService
    {

        private readonly ILogger<TransactionRunner> _logger;
        private readonly IEnumerable<IDummyRepo> _dummyrepos;
        private readonly ITransactionDelegate _transactionDelegate;

        public TransactionRunner(ILogger<TransactionRunner> logger, IEnumerable<IDummyRepo> repos, ITransactionDelegate transactionDelegate)
        {
            _logger = logger;
            _dummyrepos = repos;
            _transactionDelegate = transactionDelegate;
        }

        protected static TransactionScope GetTransactionScope()
        {
            TransactionOptions transactionOptions = new TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.MaximumTimeout
            };
            return new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, "Started...");
            Console.WriteLine("Welcome to the Demo");
            Console.WriteLine("Press Q key to exit");
            Console.WriteLine("Press 1 for succesfull commits");
            Console.WriteLine("Press 2 for one failed commit and one succesfull outside a transaction");
            Console.WriteLine("Press 3 for one failed commit and one succesfull within a transaction");
            Console.WriteLine("Press 4 for two succesfull quiries within a transaction");

            for (;;)
            {
                var consoleKeyInfo = Console.ReadKey(true);
                if (consoleKeyInfo.Key == ConsoleKey.Q)
                {
                    break;
                }

                var firstTable = RandomString(5);
                var secondTable = RandomString(6);
                var thirdTable = RandomString(6);

                var number = (int)char.GetNumericValue(consoleKeyInfo.KeyChar);
                _logger.Log(LogLevel.Information, "Started...");
                if (number == 1)
                {
                    await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo1)).DoCommit(false, firstTable);
                    await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo2)).DoCommit(false, secondTable);
                }
                if (number == 2)
                {
                    await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo1)).DoCommit(false, firstTable);
                    await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo2)).DoCommit(true, secondTable);
                }
                if (number == 3)
                {
                     var moon = _transactionDelegate.DoWorkAsync(async () =>
                     {
                         await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo1)).DoCommit(false, firstTable);
                         await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo2)).DoCommit(false, secondTable);
                         await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo2)).DoCommit(true, thirdTable);
                         _logger.Log(LogLevel.Information, "All triggered");
                     });
                    _logger.Log(LogLevel.Information, "I trigger without waiting for the above to run");
                    _logger.Log(LogLevel.Information, "before waiting for the end");
                    await moon;
                    _logger.Log(LogLevel.Information, "after waiting");

                    // Exact same as above but not using a delegate function
                    //using (var transaction = GetTransactionScope())
                    //{
                    //    try
                    //    {
                    //        var moo = await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo1)).DoCommit(false, firstTable);
                    //        var moo2 = await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo2)).DoCommit(true, secondTable);
                    //        transaction.Complete();
                    //        _logger.Log(LogLevel.Information, $"Complete {moo} {moo2}");
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        _logger.Log(LogLevel.Information, "Rolling Back");
                    //        _logger.LogError(ex.Message);
                    //    }
                    //    finally
                    //    {
                    //        _logger.Log(LogLevel.Information, "DONEEEEE");
                    //    }

                    //}
                }

                if (number == 4)
                {
                    await _transactionDelegate.DoWorkAsync(async () =>
                    {
                        var moo = await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo1)).DoCommit(false, firstTable);
                        var amh = await _dummyrepos.First(x => x.GetType() == typeof(DummyRepo2)).DoCommit(false, secondTable);

                        _logger.Log(LogLevel.Information, $"Complete {moo} {amh}");
                    });
                }

                }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
