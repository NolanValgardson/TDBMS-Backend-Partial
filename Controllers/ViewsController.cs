using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Text.Json;
using TundraApi.Utilities;

namespace TundraApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ViewsController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        public ViewsController(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        // GET api/Views
        [HttpGet]
        public async Task<IActionResult> GetViewNames()
        {
            try
            {
                var output = await GetAllViewNamesAsync();
                return Ok(output);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        // GET api/Views/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetView(string id)
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

        // POST api/Views
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement jsonBody)
        {
            /* 
             * Parameters in the stored procedure: 	
             * @ViewName NVARCHAR(128), 
             * @TableName NVARCHAR(128), 
             * @ColumnList NVARCHAR(128)
             * 
             * Json format from the frontend:
             * {
             *  "SourceTable":"Invoice",
             *  "NewTableName":"testView",
             *  "ColumnsSelected":["Col1","Col2"]
             * } 
             */

            try
            {
                // Get the columns as a JsonElement
                JsonElement ColumnsSelected = jsonBody.GetProperty("ColumnsSelected");

                // Deserialize the json into a list of the column names
                var columnList = JsonConvert.DeserializeObject<List<string>>(ColumnsSelected.ToString());
                if (columnList == null) { return BadRequest(); }

                // Create the three parameter strings for the stored procedure
                string viewName = jsonBody.GetProperty("NewTableName").ToString();
                string tableName = jsonBody.GetProperty("SourceTable").ToString();
                string columnListString = string.Join(", ", columnList);

                // Open connection to the database
                string connString = this.Configuration.GetConnectionString("Default");
                SqlConnection cnn = new(connString);
                cnn.Open();

                // Name of stored procedure
                String sql = "dbo.spCreateView";

                // Create the sql command
                SqlCommand cmd = new(sql, cnn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                cmd.Parameters.Add(new SqlParameter("@ViewName", viewName));
                cmd.Parameters.Add(new SqlParameter("@TableName", tableName));
                cmd.Parameters.Add(new SqlParameter("@ColumnList", columnListString));

                // Execute the reader for the sql command
                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                var serializedReader = ControllerHelpers.Serialize(dr);
                string jsonOutput = JsonConvert.SerializeObject(serializedReader);

                // Close all objects
                dr.Close();
                cmd.Dispose();
                cnn.Close();

                // Create new log in logs db
                new Log(Configuration, "Create/Edit View", viewName + " from " + tableName + " with columns: " + string.Join(", ", columnListString)).SendLog();

                return Ok(jsonOutput);
            }
            catch (Exception)
            {
                return BadRequest(jsonBody);
            }
        }

        // No Http put for views since editing a view is done through the post method using the same stored procedure.

        // DELETE api/Views/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            // Deserialize the json into a list of the views in the db
            var viewNamesList = JsonConvert.DeserializeObject<List<string>>(await GetAllViewNamesAsync());

            if (viewNamesList != null) {
                // varify the view is actually in the database
                foreach (var viewName in viewNamesList)
                {
                    if (viewName.ToLower().Equals(id.ToLower()))
                    {
                        try
                        {
                            // Open connection to the database
                            string connString = this.Configuration.GetConnectionString("Default");
                            SqlConnection cnn = new(connString);
                            cnn.Open();

                            // Name of stored procedure
                            String sql = "dbo.spDropView";

                            // Create the sql command
                            SqlCommand cmd = new(sql, cnn)
                            {
                                CommandType = System.Data.CommandType.StoredProcedure
                            };
                            cmd.Parameters.Add(new SqlParameter("@ViewName", id));

                            // Execute the reader for the sql command
                            SqlDataReader dr = await cmd.ExecuteReaderAsync();

                            // Close all objects
                            dr.Close();
                            cmd.Dispose();
                            cnn.Close();

                            // Create new log in logs db
                            new Log(Configuration, "Delete View", id).SendLog();

                            return Ok(new JsonResult(id));
                        }
          
                        catch (Exception)
                        {
                            return BadRequest();
                        }
                    }
                }
            }
            return BadRequest("No views found in the database!");
        }

        /* 
         * Helper methods
         */

        // connects to the db and gets all the names of the views currently in it.
        private async Task<string> GetAllViewNamesAsync()
        {
            //Open connection to the database
            string connString = this.Configuration.GetConnectionString("Default");
            SqlConnection cnn = new(connString);
            cnn.Open();

            // Name of stored procedure
            String sql = "dbo.spGetViewNames";

            // Create the sql command
            SqlCommand cmd = new(sql, cnn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            // Execute the reader for the sql command
            SqlDataReader dr = await cmd.ExecuteReaderAsync();

            // iterate through the result
            // add each to the list
            List<string> viewNamesList = new() { };
            while (dr.Read())
            {
                string? rowValue = dr.GetValue(0).ToString();
                if (rowValue != null) { viewNamesList.Add(rowValue.ToString()); }
            }

            // close all objects
            dr.Close();
            cmd.Dispose();
            cnn.Close();

            // convert list to json
            var output = JsonConvert.SerializeObject(viewNamesList);

            return output;
        }
    }
}
