using System.Net.Sockets;
using Npgsql;

class Program {
    static async Task Main(string[] args) {
        DateTime startTime = DateTime.Now;
        var retryCount = 2;
        string arguments = string.Join(" ", args);
        string[] argumentsArray = arguments.Split(' ');

        while(retryCount > 0) {
            try {
                using var conn = new NpgsqlConnection(argumentsArray[2]);
                await conn.OpenAsync();

                var userQueryData = await Database.UserDb.GetUserQueryDataAsync(conn, argumentsArray);
                var queryData = await Database.DataDb.GetQueryDataAsync(conn, argumentsArray);

                if (queryData.Count == 0) {
                    SeleniumFiller.InsertReport(
                        int.Parse(argumentsArray[0]), 
                        -1, 
                        "Puppeteer", 
                            "failure", 
                        "Cannot fetch data USER_ID: " + argumentsArray[0] + " && REPORT_DATE: " + argumentsArray[1], 
                        SeleniumFiller.CalculateElapsedTime(startTime), 
                        argumentsArray
                    );
                }

                SeleniumFiller seleniumHelper = new();
                seleniumHelper.SeleniumLoginScript(userQueryData, queryData, startTime, argumentsArray);

                conn.Close();
                break;
            } catch (Exception ex) when (ex is SocketException || ex is HttpRequestException || ex is NpgsqlException) {
                Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                retryCount--;
            } catch (Exception ex) {
                Console.WriteLine($"Exception: {ex.Message}");
                retryCount--;
            }
        }

        if (retryCount == 0) {
            SeleniumFiller.InsertReport(
                int.Parse(argumentsArray[0]), 
                -1, "Puppeteer", 
                "failure", 
                "Cannot login for USER_ID: " + argumentsArray[0] + " && REPORT_DATE: " + argumentsArray[1], 
                SeleniumFiller.CalculateElapsedTime(startTime), 
                argumentsArray
            );
            Environment.ExitCode = 0x1;
        }
    }
}