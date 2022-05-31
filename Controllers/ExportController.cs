using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TundraApi.Utilities;

namespace TundraApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        public ExportController(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        // POST api/Export
        [HttpPost]
        public async Task<IActionResult> GetCsvExport([FromBody] JsonElement jsonBody)
        {
            try
            {
                // Get the views as a JsonElement
                JsonElement ViewSelected = jsonBody.GetProperty("ViewsSelected");

                // Deserialize the json into a list of the view names
                var viewList = JsonConvert.DeserializeObject<List<string>>(ViewSelected.ToString());

                //Make csv files and return as a single zip file
                return Ok(await MakeZipFileAsync(viewList!));
            }
            catch (Exception ex)
            {
                return BadRequest("Error message: " + ex.Message);
            }
        }

        /* 
         * Helper methods
         */

        public async Task<ActionResult> MakeZipFileAsync(List<string> viewList)
        {
#pragma warning disable IDE0063 // Use simple 'using' statement
            using (var ms = new MemoryStream())
            {
                using (var archive =
                    new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    byte[] buffer = new byte[1024];

                    var viewJson = "";

                    List<string> badViews = new();

                    foreach (var view in viewList)
                    {
                        viewJson = await GetView(view);

                        if (viewJson == null) { continue; }
                        
                        DataTable? dt = (DataTable?)JsonConvert.DeserializeObject(viewJson, (typeof(DataTable)));
                        
                        if (dt == null) { continue; }

                        StringBuilder sb = new();

                        IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>()
                            .Select(column => column.ColumnName);
                        sb.AppendLine(string.Join(",", columnNames));

                        foreach (DataRow row in dt.Rows)
                        {
                            IEnumerable<string?> fields = row.ItemArray.Select(field => field?.ToString());
                            sb.AppendLine(string.Join(",", fields));
                        }

                        buffer = Encoding.ASCII.GetBytes(sb.ToString());
                        var zipEntry = archive.CreateEntry(view + ".csv", CompressionLevel.Fastest);
                        using (var zipStream = zipEntry.Open())
                        {
                            zipStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
                // Create new log in logs db
                new Log(Configuration, "Export", string.Join(", ", viewList!)).SendLog();

                // return the zip file to be sent to the user
                return File(ms.ToArray(), "application/zip", "TundraCsvDownload.zip");
            }
        }

        // Returns the json string of all the data in the specified view
        private async Task<string?> GetView(string viewName)
        {
            try
            {
                // Open connection to the database
                string connString = this.Configuration.GetConnectionString("Default");
                SqlConnection cnn = new(connString);
                cnn.Open();

                // Name of stored procedure
                String sql = "dbo.spGetAllRows";

                // Create the sql command
                SqlCommand cmd = new(sql, cnn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.Add(new SqlParameter("@TableName", viewName));

                // Execute the reader for the sql command
                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                var serializedReader = ControllerHelpers.Serialize(dr);
                var jsonOutput = JsonConvert.SerializeObject(serializedReader);

                // Close all objects
                dr.Close();
                cmd.Dispose();
                cnn.Close();

                return jsonOutput;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
