using ClosedXML.Excel;
using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Admin;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Excel
{
    public class ExcelService(
        ApplicationDbContext context,
        ILogger<ExcelService> logger) : IExcelService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<ExcelService> _logger = logger;
 
        public async Task<byte[]> GenerateTimesheetExcelAsync(int weeklyLogId, decimal hourlyRate, decimal mileageRate)
        {
            try
            {
                _logger.LogInformation("Generating Excel for weeklylog {WeeklyLogId}", weeklyLogId);
 
                var weeklyLog = await _context.Timesheet_WeeklyLogs
                    .Include(w => w.User)
                    .Include(w => w.DailyLogs.Where(d => !d.IsDeleted))
                    .FirstOrDefaultAsync(w => w.Id == weeklyLogId && !w.IsDeleted);
 
                if (weeklyLog == null)
                {
                    throw new InvalidOperationException($"WeeklyLog {weeklyLogId} not found");
                }
 
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Timesheet");
 
                // Title
                worksheet.Cell(1, 1).Value = "TIMESHEET REPORT";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 7).Merge();
 
                // Employee Information
                worksheet.Cell(3, 1).Value = "Employee:";
                worksheet.Cell(3, 1).Style.Font.Bold = true;
                worksheet.Cell(3, 2).Value = $"{weeklyLog.User.FirstName} {weeklyLog.User.LastName}";
 
                worksheet.Cell(4, 1).Value = "Email:";
                worksheet.Cell(4, 1).Style.Font.Bold = true;
                worksheet.Cell(4, 2).Value = weeklyLog.User.Email;
 
                worksheet.Cell(5, 1).Value = "Week Period:";
                worksheet.Cell(5, 1).Style.Font.Bold = true;
                worksheet.Cell(5, 2).Value = $"{weeklyLog.DateFrom:MM/dd/yyyy} - {weeklyLog.DateTo:MM/dd/yyyy}";
 
                worksheet.Cell(6, 1).Value = "Status:";
                worksheet.Cell(6, 1).Style.Font.Bold = true;
                worksheet.Cell(6, 2).Value = weeklyLog.Status.ToString();
                worksheet.Cell(6, 2).Style.Font.Bold = true;
                worksheet.Cell(6, 2).Style.Font.FontColor = weeklyLog.Status.ToString() == "Approved" 
                    ? XLColor.Green 
                    : XLColor.Black;
 
                // Summary Totals
                worksheet.Cell(8, 1).Value = "Total Hours:";
                worksheet.Cell(8, 1).Style.Font.Bold = true;
                worksheet.Cell(8, 2).Value = weeklyLog.TotalHours;
                worksheet.Cell(8, 2).Style.NumberFormat.Format = "0.00";
 
                worksheet.Cell(9, 1).Value = "Total Charges:";
                worksheet.Cell(9, 1).Style.Font.Bold = true;
                worksheet.Cell(9, 2).Value = weeklyLog.TotalCharges;
                worksheet.Cell(9, 2).Style.NumberFormat.Format = "$#,##0.00";

                var totalMileage = weeklyLog.DailyLogs.Sum(d => d.Mileage);

                worksheet.Cell(10, 1).Value = "Hourly Rate:";
                worksheet.Cell(10, 1).Style.Font.Bold = true;
                worksheet.Cell(10, 2).Value = hourlyRate;
                worksheet.Cell(10, 2).Style.NumberFormat.Format = "$#,##0.00";

                worksheet.Cell(11, 1).Value = "Gross Pay:";
                worksheet.Cell(11, 1).Style.Font.Bold = true;
                worksheet.Cell(11, 2).Value = hourlyRate * weeklyLog.TotalHours;
                worksheet.Cell(11, 2).Style.NumberFormat.Format = "$#,##0.00";

                worksheet.Cell(12, 1).Value = "Mileage Rate:";
                worksheet.Cell(12, 1).Style.Font.Bold = true;
                worksheet.Cell(12, 2).Value = mileageRate;
                worksheet.Cell(12, 2).Style.NumberFormat.Format = "$#,##0.0000";

                worksheet.Cell(13, 1).Value = "Mileage Reimbursement:";
                worksheet.Cell(13, 1).Style.Font.Bold = true;
                worksheet.Cell(13, 2).Value = mileageRate * totalMileage;
                worksheet.Cell(13, 2).Style.NumberFormat.Format = "$#,##0.00";

                // Daily Breakdown Table Header (starting at row 15)
                int currentRow = 15;
                worksheet.Cell(currentRow, 1).Value = "DAILY BREAKDOWN";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                worksheet.Range(currentRow, 1, currentRow, 7).Merge();
 
                currentRow += 2; // Row 13 for table headers
 
                // Table Headers
                var headers = new[] { "Date", "Hours", "Mileage", "Toll Charge", "Parking Fee", "Other Charges", "Comment" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(currentRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
 
                currentRow++;
 
                // Daily Log Data
                var dailyLogs = weeklyLog.DailyLogs.OrderBy(d => d.Date).ToList();
                foreach (var log in dailyLogs)
                {
                    worksheet.Cell(currentRow, 1).Value = log.Date.ToString("MM/dd/yyyy (ddd)");
                    worksheet.Cell(currentRow, 2).Value = (double)(log.TimeOut - log.TimeIn).TotalHours;
                    worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "0.00";
                    
                    worksheet.Cell(currentRow, 3).Value = log.Mileage;
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "0.00";
                    
                    worksheet.Cell(currentRow, 4).Value = log.TollCharge;
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "$#,##0.00";
                    
                    worksheet.Cell(currentRow, 5).Value = log.ParkingFee;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    
                    worksheet.Cell(currentRow, 6).Value = log.OtherCharges;
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "$#,##0.00";
                    
                    worksheet.Cell(currentRow, 7).Value = log.Comment;
 
                    // Apply borders to data rows
                    for (int col = 1; col <= 7; col++)
                    {
                        worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
 
                    currentRow++;
                }
 
                // Adjust column widths
                worksheet.Column(1).Width = 20; // Date
                worksheet.Column(2).Width = 10; // Hours
                worksheet.Column(3).Width = 10; // Mileage
                worksheet.Column(4).Width = 12; // Toll Charge
                worksheet.Column(5).Width = 12; // Parking Fee
                worksheet.Column(6).Width = 14; // Other Charges
                worksheet.Column(7).Width = 30; // Comment
 
                // Add manager comment if exists
                if (!string.IsNullOrEmpty(weeklyLog.ManagerComment))
                {
                    currentRow += 2;
                    worksheet.Cell(currentRow, 1).Value = "Manager Comment:";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = weeklyLog.ManagerComment;
                    worksheet.Range(currentRow, 1, currentRow, 7).Merge();
                    worksheet.Cell(currentRow, 1).Style.Alignment.WrapText = true;
                }
 
                // Convert to byte array
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var excelBytes = stream.ToArray();
 
                _logger.LogInformation("Successfully generated Excel for weeklylog {WeeklyLogId}", weeklyLogId);

                return excelBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel for weeklylog {WeeklyLogId}", weeklyLogId);
                throw;
            }
        }

        public Task<byte[]> GeneratePayrollSummaryExcelAsync(PayrollSummaryResponse summary)
        {
            try
            {
                _logger.LogInformation("Generating payroll summary Excel for {DateFrom} to {DateTo}", summary.DateFrom, summary.DateTo);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Payroll Summary");

                // Title
                worksheet.Cell(1, 1).Value = "PAYROLL SUMMARY";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 12).Merge();

                // Period
                worksheet.Cell(2, 1).Value = "Pay Period:";
                worksheet.Cell(2, 1).Style.Font.Bold = true;
                worksheet.Cell(2, 2).Value = $"{summary.DateFrom:MM/dd/yyyy} - {summary.DateTo:MM/dd/yyyy}";
                worksheet.Range(2, 2, 2, 12).Merge();

                // Table headers (row 4)
                int headerRow = 4;
                var headers = new[]
                {
                    "Full Name", "Employee Type", "Hourly Rate", "Mileage Rate",
                    "Total Hours", "Total Mileage", "Toll Charges", "Parking Fees", "Other Charges",
                    "Gross Pay", "Mileage Reimbursement", "Total Pay"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Alignment.WrapText = true;
                }

                // Data rows
                int currentRow = headerRow + 1;
                foreach (var item in summary.Items)
                {
                    worksheet.Cell(currentRow, 1).Value = item.FullName;
                    worksheet.Cell(currentRow, 2).Value = item.EmployeeType;

                    worksheet.Cell(currentRow, 3).Value = item.HourlyRate;
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "$#,##0.00";

                    worksheet.Cell(currentRow, 4).Value = item.MileageRate;
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "$#,##0.0000";

                    worksheet.Cell(currentRow, 5).Value = item.TotalHours;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "0.00";

                    worksheet.Cell(currentRow, 6).Value = item.TotalMileage;
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "0.00";

                    worksheet.Cell(currentRow, 7).Value = item.TotalTollCharges;
                    worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "$#,##0.00";

                    worksheet.Cell(currentRow, 8).Value = item.TotalParkingFees;
                    worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "$#,##0.00";

                    worksheet.Cell(currentRow, 9).Value = item.TotalOtherCharges;
                    worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "$#,##0.00";

                    worksheet.Cell(currentRow, 10).Value = item.GrossPay;
                    worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "$#,##0.00";

                    worksheet.Cell(currentRow, 11).Value = item.MileageReimbursement;
                    worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "$#,##0.00";

                    worksheet.Cell(currentRow, 12).Value = item.GrossPay + item.MileageReimbursement;
                    worksheet.Cell(currentRow, 12).Style.NumberFormat.Format = "$#,##0.00";

                    for (int col = 1; col <= 12; col++)
                        worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    currentRow++;
                }

                // Totals row
                if (summary.Items.Count > 0)
                {
                    worksheet.Cell(currentRow, 1).Value = "TOTALS";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 5).Value = summary.Items.Sum(i => i.TotalHours);
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(currentRow, 5).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 6).Value = summary.Items.Sum(i => i.TotalMileage);
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(currentRow, 6).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 7).Value = summary.Items.Sum(i => i.TotalTollCharges);
                    worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Cell(currentRow, 7).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 8).Value = summary.Items.Sum(i => i.TotalParkingFees);
                    worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Cell(currentRow, 8).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 9).Value = summary.Items.Sum(i => i.TotalOtherCharges);
                    worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Cell(currentRow, 9).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 10).Value = summary.Items.Sum(i => i.GrossPay);
                    worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Cell(currentRow, 10).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 11).Value = summary.Items.Sum(i => i.MileageReimbursement);
                    worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Cell(currentRow, 11).Style.Font.Bold = true;

                    worksheet.Cell(currentRow, 12).Value = summary.Items.Sum(i => i.GrossPay + i.MileageReimbursement);
                    worksheet.Cell(currentRow, 12).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Cell(currentRow, 12).Style.Font.Bold = true;

                    for (int col = 1; col <= 12; col++)
                        worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Column widths
                worksheet.Column(1).Width = 25;  // Full Name
                worksheet.Column(2).Width = 16;  // Employee Type
                worksheet.Column(3).Width = 14;  // Hourly Rate
                worksheet.Column(4).Width = 14;  // Mileage Rate
                worksheet.Column(5).Width = 14;  // Total Hours
                worksheet.Column(6).Width = 14;  // Total Mileage
                worksheet.Column(7).Width = 14;  // Toll Charges
                worksheet.Column(8).Width = 14;  // Parking Fees
                worksheet.Column(9).Width = 14;  // Other Charges
                worksheet.Column(10).Width = 14; // Gross Pay
                worksheet.Column(11).Width = 22; // Mileage Reimbursement
                worksheet.Column(12).Width = 14; // Total Pay

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var excelBytes = stream.ToArray();

                _logger.LogInformation("Successfully generated payroll summary Excel for {DateFrom} to {DateTo}", summary.DateFrom, summary.DateTo);

                return Task.FromResult(excelBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payroll summary Excel for {DateFrom} to {DateTo}", summary.DateFrom, summary.DateTo);
                throw;
            }
        }
    }
}