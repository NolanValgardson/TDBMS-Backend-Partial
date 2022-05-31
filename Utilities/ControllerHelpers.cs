using Microsoft.Data.SqlClient;

namespace TundraApi.Controllers
{
    internal static class ControllerHelpers
    {

        // serializes the SqlDataReader
        internal static IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                cols.Add(reader.GetName(i));
            }

            while (reader.Read())
            {
                results.Add(SerializeRow(cols, reader));
            }

            return results;
        }

        // serializes each row from the SqlDataReader
        private static Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SqlDataReader reader)
        {
            var result = new Dictionary<string, object>();

            foreach (var col in cols)
            {
                result.Add(col, reader[col]);
            }

            return result;
        }

    }
}