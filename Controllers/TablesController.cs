using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace TundraApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        public TablesController(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        // Get api/Tables
        [HttpGet] 
        public async Task<IActionResult> GetTableNames()
        {
            try
            {
                //Open connection to the database
                string connString = this.Configuration.GetConnectionString("Default");
                SqlConnection cnn = new(connString);
                cnn.Open();

                // Name of stored procedure
                String sql = "dbo.spGetTableNames";

                // Create the sql command
                SqlCommand cmd = new(sql, cnn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                // Execute the reader for the sql command
                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                //iterate through the result
                //add each to the list
                List<string> tableNamesList = new() { };
                while (dr.Read())
                {
                    string? rowValue = dr.GetValue(0).ToString();
                    if (rowValue != null) { tableNamesList.Add(rowValue); }
                }

                //close all objects
                dr.Close();
                cmd.Dispose();
                cnn.Close();

                // convert list to json
                var output = JsonConvert.SerializeObject(tableNamesList);

                return Ok(output);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        // GET api/Tables/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTable(string id)
        {
            try
            {
                // Open connection to the database
                string connString = this.Configuration.GetConnectionString("Default");
                SqlConnection cnn = new(connString);
                cnn.Open();

                // Name of stored procedure
                String sql = "dbo.spGetPreview";

                // Create the sql command
                SqlCommand cmd = new(sql, cnn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                cmd.Parameters.Add(new SqlParameter("@TableName", id));

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
            catch
            {
                return NotFound();
            }
        }
    }
}