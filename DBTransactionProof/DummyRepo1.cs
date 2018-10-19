using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace DBTransactionProof
{
    public class DummyRepo1 : IDummyRepo
    {
        private readonly ILogger<DummyRepo1> _logger;
        public DummyRepo1(ILogger<DummyRepo1> logger)
        {
            _logger = logger;
        }

        public async Task<int> DoCommit(bool fail, string name)
        {
            _logger.LogInformation($"Comitted repo 1 {(fail ? "EMPLOYEE" : name)}");
            using (var con = new SqlConnection("Server=(local);Database=Deploy_logins;Integrated Security=true;"))
            {
                try
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
                catch (System.Exception ex)
                {
                    _logger.LogInformation($"Fluffed it in the repo 1: {ex.Message}");
                    throw ex;
                }
                
            }

            _logger.LogInformation($"Comitted repo 1 {(fail ? "EMPLOYEE" : name)}");
        }
    }
}
