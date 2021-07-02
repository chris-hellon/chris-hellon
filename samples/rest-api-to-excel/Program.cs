using Binance.API;
using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;

namespace Binance
{
    static class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-GB");

            Console.WriteLine("Please enter a From Date in DD/MM/YYYY format, or press the enter key to default to last month.");

            DateTime fromDate = new DateTime();
            DateTime toDate = new DateTime();
            
            string fromDateInput = "";

            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(false);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    fromDateInput = "d";
                    break;
                }
                else break;
            } while (true);

            if (fromDateInput != "d") fromDateInput = keyInfo.KeyChar.ToString().ToLower() + Console.ReadLine();

            string fromDateOutput = "";
            DetectInput(fromDateInput, ref fromDateOutput);

            if (fromDateOutput.Length > 0)
            {
                CheckDateFormat(fromDateOutput, true, ref fromDate);

                Console.WriteLine("");
                Console.WriteLine("Please enter a To Date in DD/MM/YYYY format, or press the enter key to default to last month.");

                string toDateInput = "";

                ConsoleKeyInfo consoleKey = Console.ReadKey();
                if (consoleKey.Key == ConsoleKey.Enter) toDateInput = "d";
                else toDateInput = consoleKey.KeyChar + Console.ReadLine();

                string toDateOutput = "";
                DetectInput(toDateInput, ref toDateOutput);

                if (toDateOutput.Length > 0)
                {
                    CheckDateFormat(toDateOutput, false, ref toDate);

                    Console.WriteLine("");
                    Console.WriteLine("To run the query for both USD-M AND COIN-M, press the 'b' key. For USD-M only, the 'u' key. And for Coin-M only, the 'c' key.");

                    ConsoleKeyInfo consoleKeyOutput = new ConsoleKeyInfo();
                    consoleKey = Console.ReadKey();

                    CheckConsoleKey(consoleKey, new ConsoleKey[] { ConsoleKey.B, ConsoleKey.U, ConsoleKey.C }, ref consoleKeyOutput);

                    switch (consoleKeyOutput.Key)
                    {
                        case ConsoleKey.B:
                            RunQuery(fromDate, toDate, true);
                            RunQuery(fromDate, toDate, false);
                            break;
                        case ConsoleKey.U:
                            RunQuery(fromDate, toDate, true);
                            break;
                        case ConsoleKey.C:
                            RunQuery(fromDate, toDate, false);
                            break;
                    }

                    Console.WriteLine("");
                    Console.WriteLine("All records returned successfully. Press any key to exit.");
                    Console.ReadKey();
                }
            }
        }

        static void RunQuery(DateTime fromDate, DateTime toDate, bool usdM)
        {
            Console.WriteLine("");

            if (usdM) Console.WriteLine("Please enter a comma separated list of USD-M symbols, eg. BTCUSDT,ETHUSDT,DOTUSDT or press the enter key to use all available tokens.");
            else Console.WriteLine("Please enter a comma separated list of COIN-M symbols, eg. BTCUSD_PERP,ETHUSD_PERP,DOTUSD_PERP or press the enter key to use all available tokens.");

            string symbolsInput = "";

            ConsoleKeyInfo consoleKey = Console.ReadKey();
            if (consoleKey.Key == ConsoleKey.Enter) symbolsInput = "d";
            else symbolsInput = consoleKey.KeyChar + Console.ReadLine();

            string symbolsOutput = "";
            DetectInput(symbolsInput, ref symbolsOutput);

            string[] symbolsArray = null;

            if (symbolsOutput.Length > 0 && symbolsOutput != "d")
            {
                symbolsOutput = symbolsOutput.ToUpper().Trim().TrimEnd(',');
                symbolsArray = symbolsOutput.Split(',');
            }

            FundingRates(fromDate, toDate, symbolsArray, usdM);
        }
        static ConsoleKeyInfo CheckConsoleKey(ConsoleKeyInfo input, ConsoleKey[] consoleKeys, ref ConsoleKeyInfo output)
        {
            if (!consoleKeys.Contains(input.Key))
            {
                Console.WriteLine("");
                string message = "Please enter either the ";
                foreach (var key in consoleKeys)
                {
                    message += key.ToString() + ", ";
                }

                message = message.Trim();
                message = message.TrimEnd(',');
                message += " key.";

                Console.WriteLine(message);

                return CheckConsoleKey(Console.ReadKey(), consoleKeys, ref output);
            }
            else output = input;

            return output;
        }

        static void FundingRates(DateTime fromDate, DateTime toDate, string[] symbolArray = null, bool USDm = true)
        {
            API.Futures.Client apiClient = null;

            if (USDm) apiClient = new API.Futures.Client("https://fapi.binance.com/fapi/v1/");
            else apiClient = new API.Futures.Client("https://dapi.binance.com/dapi/v1/");

            int limit = 1000;

            Console.WriteLine("");
            Console.WriteLine("Running query for " + (USDm ? "USD-M" : "Coin-M"));

            if (symbolArray == null)
            {
                Console.WriteLine("");
                Console.WriteLine("Retrieving Binance Exchange Information");

                // GET LATEST EXCHANGE INFO FROM BINANCE API
                BaseResponseDTO<Models.ExchangeInfoModel> exchangeInfo = apiClient.GetExchangeInfo();

                if (exchangeInfo.Success)
                {
                    Console.WriteLine("Exchange Information retrieved");
                    symbolArray = exchangeInfo.Data.Symbols.Select(w => w.Symbol).ToArray();
                }
            }

            if (symbolArray.Length > 0)
            {
                using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
                {
                    // CREATE SPREADSHEET DOCUMENT
                    SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

                    WorkbookPart workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();

                    Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());

                    Console.WriteLine("");
                    Console.WriteLine("Retrieving data for all available tokens");

                    int successCount = 0;

                    Dictionary<string, List<Models.FundingRateModel>> tokenFundingRates = new Dictionary<string, List<Models.FundingRateModel>>();

                    List<string> years = new List<string>();
                    List<string> months = new List<string>();
                    List<string> days = new List<string>();

                    for (int s = 0; s < symbolArray.Length; s++)
                    {
                        string symbol = symbolArray[s];

                        // GET FUNDING RATES FOR SYMBOL / TOKEN
                        List<Models.FundingRateModel> fundingRates = new List<Models.FundingRateModel>();
                        GetFundingRatesRecursive(apiClient, symbol, fromDate, toDate, limit, ref fundingRates);

                        if (fundingRates.Count > 0)
                        {
                            successCount++;
                            tokenFundingRates.Add(symbol, fundingRates);

                            List<DateTime> fundingRatesDates = fundingRates.Select(w => w.FundingTimeParsed).ToList();

                            years.AddRange(fundingRatesDates.Select(w => w.Year.ToString()).ToList().Where(w => !years.Contains(w)));
                            months.AddRange(fundingRatesDates.Select(w => w.ToString("MMMM") + " " + w.Year.ToString()).ToList().Where(w => !months.Contains(w)));
                            days.AddRange(fundingRatesDates.Select(w => w.ToShortDateString()).ToList().Where(w => !days.Contains(w)));
                        }
                    }

                    string[] sheetArray = new[] { "Daily", "Monthly", "Yearly" };
                    int index = 1;

                    for (int i = 0; i < sheetArray.Length; i++)
                    {
                        string sheetTitle = sheetArray[i];

                        // ADD SHEET TO WORKBOOK
                        WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

                        var sheetData = new SheetData();
                        worksheetPart.Worksheet = new Worksheet(sheetData);

                        Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = new UInt32Value((uint)i + 1), Name = sheetTitle };
                        sheets.Append(sheet);

                        // CREATE HEADER ROW
                        Row row = new Row() { RowIndex = new UInt32Value((uint)index) };
                        row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.String, CellValue = new CellValue("Date") });

                        for (int s = 0; s < symbolArray.Length; s++)
                        {
                            string symbol = symbolArray[s];
                            row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.String, CellValue = new CellValue(symbol) });
                        }

                        sheetData.AppendChild(row);
                        index++;

                        if (sheetTitle == "Daily")
                        {
                            Dictionary<string, decimal> tokenTotals = new Dictionary<string, decimal>();

                            for (int d = 0; d < days.Count; d++)
                            {
                                string day = days[d];
                                row = new Row() { RowIndex = new UInt32Value((uint)index) };
                                row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.String, CellValue = new CellValue(day) });

                                for (int s = 0; s < symbolArray.Length; s++)
                                {
                                    string symbol = symbolArray[s];

                                    List<Models.FundingRateModel> fundingRates = tokenFundingRates[symbol];

                                    decimal dailyTotal = fundingRates.Where(w => w.FundingTimeParsed.ToShortDateString() == day).Sum(w => w.FundingRate);
                                    row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.Number, CellValue = new CellValue(dailyTotal) });

                                    if (!tokenTotals.ContainsKey(symbol)) tokenTotals.Add(symbol, dailyTotal);
                                    else tokenTotals[symbol] += dailyTotal;
                                }

                                sheetData.AppendChild(row);
                                index++;
                            }
                        }

                        if (sheetTitle == "Monthly")
                        {
                            Dictionary<string, decimal> tokenTotals = new Dictionary<string, decimal>();

                            for (int m = 0; m < months.Count; m++)
                            {
                                string month = months[m];
                                row = new Row() { RowIndex = new UInt32Value((uint)index) };
                                row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.String, CellValue = new CellValue(month) });

                                for (int s = 0; s < symbolArray.Length; s++)
                                {
                                    string symbol = symbolArray[s];

                                    List<Models.FundingRateModel> fundingRates = tokenFundingRates[symbol];

                                    decimal monthlyTotal = fundingRates.Where(w => w.FundingTimeParsed.ToString("MMMM") + " " + w.FundingTimeParsed.Year.ToString() == month).Sum(w => w.FundingRate);
                                    row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.Number, CellValue = new CellValue(monthlyTotal) });

                                    if (!tokenTotals.ContainsKey(symbol)) tokenTotals.Add(symbol, monthlyTotal);
                                    else tokenTotals[symbol] += monthlyTotal;
                                }

                                sheetData.AppendChild(row);
                                index++;
                            }
                        }

                        if (sheetTitle == "Yearly")
                        {
                            Dictionary<string, decimal> tokenTotals = new Dictionary<string, decimal>();

                            for (int y = 0; y < years.Count; y++)
                            {
                                string year = years[y];
                                row = new Row() { RowIndex = new UInt32Value((uint)index) };
                                row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.String, CellValue = new CellValue(year) });

                                for (int s = 0; s < symbolArray.Length; s++)
                                {
                                    string symbol = symbolArray[s];

                                    List<Models.FundingRateModel> fundingRates = tokenFundingRates[symbol];

                                    decimal yearlyTotal = fundingRates.Where(w => w.FundingTimeParsed.Year.ToString() == year).Sum(w => w.FundingRate);
                                    row.AppendChild(new Cell() { StyleIndex = new UInt32Value(), DataType = CellValues.Number, CellValue = new CellValue(yearlyTotal) });

                                    if (!tokenTotals.ContainsKey(symbol)) tokenTotals.Add(symbol, yearlyTotal);
                                    else tokenTotals[symbol] += yearlyTotal;
                                }

                                sheetData.AppendChild(row);
                                index++;
                            }
                        }

                        index = 1;
                    }

                    index++;

                    workbookPart.Workbook.Save();
                    document.Close();

                    string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    filePath = filePath + @"\" + (USDm ? "USD-M" : "COIN-M") + "_FundingRates_" + fromDate.ToShortDateString().Replace("/", "") + "-" + toDate.AddDays(-1).ToShortDateString().Replace("/", "") + "-" + Guid.NewGuid().ToString() + ".xlsx";

                    FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    memoryStream.WriteTo(file);
                    file.Close();
                    memoryStream.Close();

                    Console.WriteLine("");
                    Console.WriteLine(successCount + " tokens retrieved in total");
                }
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("No Tokens found.");
            }
        }

        static List<Models.FundingRateModel> GetFundingRatesRecursive(API.Futures.Client apiClient, string symbol, DateTime fromDate, DateTime toDate, int limit, ref List<Models.FundingRateModel> fundingRates)
        {
            BaseResponseDTO<List<Models.FundingRateModel>> response = apiClient.GetFundingRates(symbol, fromDate, toDate, limit);

            if (response.Success)
            {
                response.Data.Select(c => { c.FundingTimeParsed = c.FundingTime.ParseLongToDate().AddHours(-1); return c; }).ToList();

                fundingRates.AddRange(response.Data);

                int responseCount = response.Data.Count;

                if (responseCount == limit)
                {
                    DateTime lastDateTime = response.Data[responseCount - 1].FundingTime.ParseLongToDate();
                    return GetFundingRatesRecursive(apiClient, symbol, lastDateTime, toDate, limit, ref fundingRates);
                }
            }

            return fundingRates;
        }
        
        static string DetectInput(string input, ref string output)
        {
            if (input.Length == 0)
            {
                Console.WriteLine("No input detected, please enter an input.");
                return DetectInput(Console.ReadLine(), ref output);
            }
            else output = input;

            return output;
        }
        static DateTime CheckDateFormat(string date, bool start, ref DateTime returnDate)
        {
            if (date != "d")
            {
                if (!DateTime.TryParse(date, out returnDate))
                {
                    Console.WriteLine("Date is not in correct format, please use format DD/MM/YYYY, or enter 'd' to use the default date.");
                    return CheckDateFormat(Console.ReadLine(), start, ref returnDate);
                }
            }
            else
            {
                if (start)
                {
                    returnDate = DateTime.Now.AddMonths(-1);
                    TimeSpan ts = new TimeSpan(07, 0, 0);
                    returnDate = returnDate.Date + ts;
                }
                else
                {
                    returnDate = DateTime.Now.AddDays(1);
                    TimeSpan ts = new TimeSpan(0, 0, 0);
                    returnDate = returnDate.Date + ts;
                }
            }

            return returnDate;
        }

        static Row GetRow(SheetData sheetData, int index)
        {
            Row row = null;

            int childCount = sheetData.ChildElements.Count;
            if ((index + 1) > childCount)
            {
                row = new Row() { RowIndex = new UInt32Value((uint)index) };
                sheetData.AppendChild(row);
            }
            else row = sheetData.ChildElements[index] as Row;

            return row;
        }
        static Cell GetCell(EnumValue<CellValues> dataType, object value)
        {
            return new Cell() { DataType = dataType, CellValue = new CellValue(value.ToString()) };
        }

        static void AppendCells(this Row row, List<Cell> cells)
        {
            foreach (var cell in cells)
            {
                row.AppendChild(cell);
            }
        }

        static uint InsertBorder(WorkbookPart workbookPart, Border border)
        {
            Borders borders = workbookPart.WorkbookStylesPart.Stylesheet.Elements<Borders>().First();
            borders.Append(border);
            return (uint)borders.Count++;
        }
        static Border GenerateBorder()
        {
            Border border = new Border();

            LeftBorder leftBorder = new LeftBorder() { Style = BorderStyleValues.Thin };
            Color color = new Color() { Indexed = (UInt32Value)64U };

            leftBorder.Append(color);

            RightBorder rightBorder = new RightBorder() { Style = BorderStyleValues.Thin };
            color = new Color() { Indexed = (UInt32Value)64U };

            rightBorder.Append(color);

            TopBorder topBorder = new TopBorder() { Style = BorderStyleValues.Thin };
            color = new Color() { Indexed = (UInt32Value)64U };

            topBorder.Append(color);

            BottomBorder bottomBorder = new BottomBorder() { Style = BorderStyleValues.Thin };
            color = new Color() { Indexed = (UInt32Value)64U };

            bottomBorder.Append(color);
            DiagonalBorder diagonalBorder = new DiagonalBorder();

            border.Append(leftBorder);
            border.Append(rightBorder);
            border.Append(topBorder);
            border.Append(bottomBorder);
            border.Append(diagonalBorder);

            return border;
        }
    }
}
