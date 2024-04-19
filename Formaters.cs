namespace Extensions {
    public static class FormaterMethods {
        public static decimal FormatDecimal(decimal value) {
            return Math.Round(value, 2);
        }

        public static string FormatDate(DateTime date) {
            try {
                return date.ToString("dd.MM.yyyy");
            }
            catch (Exception ex) {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                throw;
            }
        }
    }
}