using Microsoft.Data.SqlClient;
using System.Data;

namespace TundraApi.Utilities
{
    public class Log
    {
        private readonly IConfiguration Configuration;

        private readonly string sqlFormattedDate = "";
        private string Action { get; set; }
        private string Description { get; set; }

        /*
         * Log takes an action and description to allow the SendLog method to send a log to the logs database.
         * NOTE: Both action and description are required.
         */
        public Log(IConfiguration _configuration, string action, string description)
        {
            Configuration = _configuration;

            DateTime myDateTime = DateTime.Now;
            sqlFormattedDate = myDateTime.ToString("yyyyMMdd HH:mm:ss.fff");

            this.Action = action;
            this.Description = description;
        }

        // Sends a log to the log database
        public async void SendLog()
        {
            /*
             * Stored Procedure parameters:
             * @Date datetime,
             * @Action NVARCHAR(50),
             * @Description NVARCHAR(200)
             */

            // Open connection to the database
            string connString = Configuration.GetConnectionString("LogDb");
            SqlConnection cnn = new(connString);
            cnn.Open();

            // Initialization
            SqlCommand cmd;
            SqlDataReader dr;
            string sql;

            // Name of stored procedure
            sql = "dbo.spNewLog";

            // Create the sql command
            cmd = new SqlCommand(sql, cnn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@Date", sqlFormattedDate));
            cmd.Parameters.Add(new SqlParameter("@Action", Action));
            cmd.Parameters.Add(new SqlParameter("@Description", Description));

            // Execute the reader for the sql command
            dr = await cmd.ExecuteReaderAsync();

            // Close all objects
            dr.Close();
            cmd.Dispose();
            cnn.Close();
        }
    }
}

