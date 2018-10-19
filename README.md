# Calling multiple quiries under a single transaction scope - using dapper #

A simple example project - please ignore the terrible code implementations.

Simply put, this gives an asyc example of creating a transactionscope where you can call multiple repositories even with the same connection strings and have it both bubble up errors and roll back on failure.

Just download and run, you have the options for 4 inputs numbered 1-4.

3 & 4 and the ones wrapped in a transaction.
3 has a set failure, 4 has two success operations, both attempt to create tables with random names (You will need to change the DBconnection string on the repos.)

## TransactionScope ##
A TransactionScope wrapping asynchronous code needs to specify ```TransactionScopeAsyncFlowOption.Enabled``` in its constructor.

```c#
protected TransactionScope GetTransactionScope()
{
    return new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
}
```
The transactionscope provides a model in which the infastructure automatically manages the trasnactions for you.
Any connection opened within the transaction is automatically part of that transaction and should an error rise during the execution of those quiries will trigger a rollback automatically.

Repeated errors of things such as the connection closing before the code executes or even more sporadic: [This platform does not support distributed transactions](https://github.com/dotnet/corefx/issues/29633)

This all came from the trouble getting the execution order correct. 
The quiries must execute before the transaction executes.

The whole design point here is to have the ability to query multiple repos, multiple dbs without having all the code sharing a single connection or all in the same repo or directly connected!
Simply to start a transaction, run quiries and roll them all back reguardless of the repo used.

## The Delegate Function ##

The point is to make this usable whereever its required so basically the ability to execute your quiries within a transaction created for you:

```c#
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
```

This is the rough structure. Some logs to see where things are getting but the core here is creating the transaction and allowing me to pass in and run any required logic.

```c#
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
```

Like so, now with this example my quiries execute, my code continues until I await moon and I maintain the correct level of asyc behaviour.

The key to this working is how the repositories handle their async calls.

### Example 1 ###

```c#
public async Task<int> DoCommit(bool fail, string name)
{
    _logger.LogInformation($"Comitted repo 2 {(fail ? "EMPLOYEE" : name)}");
    try
    {
        using (var con = new SqlConnection("Server=(local);Database=Deploy_logins;Integrated Security=true;"))
        {
            var i = await con.ExecuteAsync(@"CREATE TABLE [" + (fail ? "EMPLOYEE" : name) + @"] (
                        PersonID int,
                        LastName varchar(255),
                        FirstName varchar(255),
                        Address varchar(255),
                        City varchar(255) 
                    );");
            return i;
        }
    }
    catch (System.Exception ex)
    {
        _logger.LogInformation($"Fluffed it in the repo 2: {ex.Message}");
        throw ex;
    }
}
```

This has some fluff just for identifying where things explode and allows me to pass a bool to fail or not as I know I have a table by that name already.
The key here, returning an actual value.
If you actualise the value the system wont go off and thread this to its whim and have it execute prior to its own running within the async command setup for the very trasnaction its supposed to run within!

By returning an actual value (a bool would do) the code will execute async on the same thread as the other DB quiries within the transaction, mainiting the required order of execution but not blocking logic outside of the transaction!!
And all without having the repos know a thing about that transaction iteself!

This took a whole day sooo ... there it is!