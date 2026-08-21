namespace RayanTask.Helpers;

public static class PersianDateHelper
{
    public static string ToPersianDate(this DateTime date)
    {
        var persianCalendar = new System.Globalization.PersianCalendar();
        var year = persianCalendar.GetYear(date);
        var month = persianCalendar.GetMonth(date);
        var day = persianCalendar.GetDayOfMonth(date);

        return $"{year}/{month:D2}/{day:D2}";
    }

    public static string ToPersianDateWithDayName(this DateTime date)
    {
        var persianCalendar = new System.Globalization.PersianCalendar();
        var year = persianCalendar.GetYear(date);
        var month = persianCalendar.GetMonth(date);
        var day = persianCalendar.GetDayOfMonth(date);
        var dayOfWeek = persianCalendar.GetDayOfWeek(date);

        string dayName = "";
        switch (dayOfWeek)
        {
            case DayOfWeek.Saturday:
                dayName = "شنبه";
                break;
            case DayOfWeek.Sunday:
                dayName = "یکشنبه";
                break;
            case DayOfWeek.Monday:
                dayName = "دوشنبه";
                break;
            case DayOfWeek.Tuesday:
                dayName = "سه‌شنبه";
                break;
            case DayOfWeek.Wednesday:
                dayName = "چهارشنبه";
                break;
            case DayOfWeek.Thursday:
                dayName = "پنج‌شنبه";
                break;
            case DayOfWeek.Friday:
                dayName = "جمعه";
                break;
        }

        return $"{dayName} {year}/{month:D2}/{day:D2}";
    }
}

