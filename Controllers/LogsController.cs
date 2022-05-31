using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace TundraApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        public LogsController(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        // GET api/Logs
        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            try
            {
                // Open connection to the database
                string connString = this.Configuration.GetConnectionString("LogDb");
                SqlConnection cnn = new(connString);
                cnn.Open();

                // Name of stored procedure
                String sql = "dbo.spGetLogs";

                // Create the sql command
                SqlCommand cmd = new(sql, cnn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                // Execute the reader for the sql command
                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                var serializedReader = ControllerHelpers.Serialize(dr);
                string jsonOutput = JsonConvert.SerializeObject(serializedReader);

                // Close all objects
                dr.Close();
                cmd.Dispose();
                cnn.Close();

                return Ok(jsonOutput);
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex);
            }
            
        }
    }
}
