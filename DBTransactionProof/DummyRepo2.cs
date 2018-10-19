using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace DBTransactionProof
{
    public class DummyRepo2 : IDummyRepo
    {
        private readonly ILogger<DummyRepo2> _logger;
        public DummyRepo2(ILogger<DummyRepo2> logger)
        {
            _logger = logger;
        }

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
    }
}
