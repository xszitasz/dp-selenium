using Extensions;
using Npgsql;

namespace Database {
    public class DataDb {
        public static async Task<List<Dictionary<string, object>>> GetQueryDataAsync(NpgsqlConnection conn, string[] argumentsArray) {
            try {
                // Query with DOUBLE
                var sql = $"SELECT * FROM ec WHERE \"DatumInputField\" = '{argumentsArray[1]}' AND user_id = '{argumentsArray[0]}' AND user_id = (SELECT user_id FROM users WHERE user_id = '{argumentsArray[0]}')";
                
                // Query with INT
                // var sql = $"SELECT * FROM ec_error_int WHERE \"DatumInputField\" = '{argumentsArray[1]}' AND user_id = '{argumentsArray[0]}' AND user_id = (SELECT user_id FROM users WHERE user_id = '{argumentsArray[0]}')";

                // Query with TEXT
                // var sql = $"SELECT * FROM ec_error_text WHERE \"DatumInputField\" = '{argumentsArray[1]}' AND user_id = '{argumentsArray[0]}' AND user_id = (SELECT user_id FROM users WHERE user_id = '{argumentsArray[0]}')";

                // Query with NULL
                // var sql = $"SELECT * FROM ec_error_null WHERE \"DatumInputField\" = '{argumentsArray[1]}' AND user_id = '{argumentsArray[0]}' AND user_id = (SELECT user_id FROM users WHERE user_id = '{argumentsArray[0]}')";
                var queryData = new List<Dictionary<string, object>>();

                using (var cmd = new NpgsqlCommand(sql, conn)) {
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) {
                        var item = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++) {
                            string fieldName = reader.GetName(i);
                            object value = reader[i];

                            if (value is decimal decimalValue) {
                                value = FormaterMethods.FormatDecimal(decimalValue);
                            } else if (value is DateTime dateTimeValue) {
                                value = FormaterMethods.FormatDate(dateTimeValue);
                            }
                            item.Add(fieldName, value);
                        }
                        queryData.Add(item);
                    }
                }
                return queryData;
            } catch (NpgsqlException ex) {
                Console.WriteLine($"NpgsqlException occurred while fetching query data: {ex.Message}");
                throw;
            } catch (Exception ex) {
                Console.WriteLine($"Exception occurred while fetching query data: {ex.Message}");
                throw;
            }
        }
    }

    public class UserDb {
        public static async Task<List<Dictionary<string, object>>> GetUserQueryDataAsync(NpgsqlConnection conn, string[] argumentsArray) {
            var userData = $"SELECT user_name, pwd FROM users WHERE user_id = {argumentsArray[0]}";
            var userQueryData = new List<Dictionary<string, object>>();

            try {
                using (NpgsqlCommand userCmd = new(userData, conn)) {
                    using NpgsqlDataReader userReader = await userCmd.ExecuteReaderAsync();
                    while (await userReader.ReadAsync()) {
                        var item = new Dictionary<string, object>();

                        for (int i = 0; i < userReader.FieldCount; i++) {
                            string fieldName = userReader.GetName(i);
                            object value = userReader[i];
                            item.Add(fieldName, value);
                        }
                        userQueryData.Add(item);
                    }
                }
                return userQueryData;
            } catch(NpgsqlException ex) {
                Console.WriteLine($"NpgsqlException occurred while fetching user query data: {ex.Message}");
                throw;
            }
            catch (Exception ex) {
                Console.WriteLine($"Exception occurred while fetching user query data: {ex.Message}");
                throw;
            }
        }
    }
}