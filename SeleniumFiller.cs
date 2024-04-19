using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Npgsql;
class SeleniumFiller {
    private readonly ChromeOptions _driverOptions;

    public SeleniumFiller() {
        _driverOptions = new ChromeOptions();
        _driverOptions.AddArguments("--disable-console", "--profile-directory=Default", "--disable-popup-blocking", "--disable-background-networking", "--disable-gpu", "--no-sandbox", "--disable-dev-shm-usage", "--start-maximized", "--disable-infobars", "--disable-extensions", "--detach");
    }

    private static string FormatTimeSpan(TimeSpan timeSpan) {
        int minutes = (int)timeSpan.TotalMinutes;
        double seconds = timeSpan.TotalSeconds - (minutes * 60);
        return $"minutes: {minutes} seconds: {seconds:F3}";
    }

    private static void ClickRadioButtonGroup(IWebDriver driver, WebDriverWait wait, string cssSelector) {
        try {
            IWebElement radioButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector(cssSelector)));
            radioButton.Click();
        } catch (WebDriverException e) {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
            throw;
        }
    }

    private static void ClickButton(IWebDriver driver, string id) {
        try {
            IWebElement button = driver.FindElement(By.Id(id));
            button.Click();
        } catch (WebDriverException e) {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
            throw;
        }
    }

    private static void FillInputField(IWebDriver driver, string id, string text) {
        try {
            IWebElement inputField = driver.FindElement(By.Id(id));
            inputField.Click();
            inputField.SendKeys(text);
        } catch (WebDriverException e) {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
            throw;
        }
    }

    public static string CalculateElapsedTime(DateTime startTime) {
        DateTime endTime = DateTime.Now;
        TimeSpan elapsedTime = endTime - startTime;
        return FormatTimeSpan(elapsedTime);
    }

    public void SeleniumLoginScript(List<Dictionary<string, object>> userQueryData, List<Dictionary<string, object>> queryData, DateTime startTime, string[] argumentsArray) {
        int loginErrorCount = 2;

        while(loginErrorCount > 0) {
            try {
                using var driver = new ChromeDriver(_driverOptions);
                driver.Navigate().GoToUrl("https://www.diportal.sk/statistika/nahlasovanie");
                driver.SwitchTo().Frame(0);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                try {
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.TagName("body"))).Click();
                } catch (WebDriverException e) {
                    loginErrorCount--;
                    Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                }

                FillInputField(driver, "EKAC.Login.LoginInputField", userQueryData.FirstOrDefault(x => x.ContainsKey("user_name"))?["user_name"].ToString() ?? "");
                FillInputField(driver, "EKAC.Login.PasswordInputField", userQueryData.FirstOrDefault(x => x.ContainsKey("pwd"))?["pwd"].ToString() ?? "");

                try {
                    ClickButton(driver, "EKAC.Login.Button");
                } catch (WebDriverException e) {
                    loginErrorCount--;
                    Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                }

                ClickRadioButtonGroup(driver, wait, "[id='EKAC.HeaderView.EICRadioButtonGroupByKey:0-lbl']");
                PopulateHTMLTagsWithIDs(queryData, wait, startTime, argumentsArray);

                try {
                    ClickButton(driver, "EKAC.DenneNahlasovanieView.SaveButton");
                } catch (WebDriverException e) {
                    loginErrorCount--;
                    Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                }

                driver.Close();
                break;
            } catch (Exception e) {
                loginErrorCount--;
                Console.WriteLine("General exception occurred: " + e.Message);
            }
            if (loginErrorCount == 0) {
                InsertReport(
                    int.Parse(argumentsArray[0]), 
                    -1, 
                    "Puppeteer", 
                    "failure", 
                    "Cannot fetch/login for USER_ID: " + argumentsArray[0] + " && REPORT_DATE: " + argumentsArray[1], 
                    CalculateElapsedTime(startTime),
                    argumentsArray
                );
                Environment.ExitCode = 0x1;
            }
        }
    }

    private static void PopulateHTMLTagsWithIDs(List<Dictionary<string, object>> queryData, WebDriverWait wait, DateTime startTime, string[] argumentsArray) {
        int userId = (int)queryData[0]["user_id"], consId = (int)queryData[0]["cons_id"], fillErrorCount = 2;
        HashSet<string> invalidDataTypes = new();

        while(fillErrorCount > 0) {
            try {
                foreach (var items in queryData) {
                    foreach (var entry in items.Where(e => e.Key != "cons_id" && e.Key != "user_id")) {
                        string id = "EKAC.DenneNahlasovanieView." + entry.Key;
                        var element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.Id(id)));
                        if (element != null && element.Displayed) {
                            string value = entry.Value?.ToString() ?? "null or undefined";
                            string? valueNull = entry.Value != null ? entry.Value.ToString() : "null or undefined";

                            if (entry.Value == DBNull.Value) {
                                invalidDataTypes.Add($"{entry.Key}: null or undefined | ");
                            } else if (id != "EKAC.DenneNahlasovanieView.DatumInputField") {
                                value = value.Replace(".", ",");
                                if (!decimal.TryParse(value, out _)) {
                                    invalidDataTypes.Add($"{entry.Key}: {entry.Value} | ");
                                }
                            }
                            element.Clear();
                            element.SendKeys(value);
                        }
                    }
                }
                if(invalidDataTypes.Count > 0) {
                    fillErrorCount--;
                }
            } catch (Exception ex) {
                fillErrorCount--;
                Console.WriteLine($"Exception occurred: {ex.GetType().Name}: {ex.Message}");
            }

            if(invalidDataTypes.Count == 0) {
                break;
            }
        }
        
        if(invalidDataTypes.Count > 0 || fillErrorCount < 0) {
            InsertReport(
                userId, 
                consId, 
                "Selenium", 
                "failure", 
                string.Join(" | ", invalidDataTypes.ToArray()), 
                CalculateElapsedTime(startTime),
                argumentsArray
            );
            Environment.ExitCode = 0x1;
        }
        else if(fillErrorCount == 2 || fillErrorCount == 2) {
            InsertReport(
                userId, 
                consId, 
                "Selenium", 
                "success", 
                "Report filled out", 
                CalculateElapsedTime(startTime),
                argumentsArray
            );
        }
    }

    public static void InsertReport(int userId, int consId, string techType, string flag, string reportMsg, string testTime, string[] connString) {
        using var connection = new NpgsqlConnection(connString[2]);
        connection.Open();

        using var cmd = new NpgsqlCommand();
        cmd.Connection = connection;
        cmd.CommandText = "INSERT INTO logs (user_id, cons_id, tech_type, flag, report_msg, test_time) VALUES (@userId, @consId, @techType, @flag, @reportMsg, @testTime)";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@consId", consId);
        cmd.Parameters.AddWithValue("@techType", techType);
        cmd.Parameters.AddWithValue("@flag", flag);
        cmd.Parameters.AddWithValue("@reportMsg", reportMsg);
        cmd.Parameters.AddWithValue("@testTime", testTime);
        cmd.ExecuteNonQuery();
    }
}
