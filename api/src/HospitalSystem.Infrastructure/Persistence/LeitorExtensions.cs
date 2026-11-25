using MySqlConnector;

namespace HospitalSystem.Infrastructure.Persistence;

/// <summary>Leitura segura de colunas (o banco legado tem várias colunas que aceitam NULL).</summary>
internal static class LeitorExtensions
{
    public static string Str(this MySqlDataReader r, string col) => r.StrN(col) ?? string.Empty;

    public static string? StrN(this MySqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public static int Int(this MySqlDataReader r, string col) => r.IntN(col) ?? 0;

    public static int? IntN(this MySqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : Convert.ToInt32(r.GetValue(i));
    }

    public static decimal Dec(this MySqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    }

    public static bool Bool(this MySqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return !r.IsDBNull(i) && Convert.ToInt32(r.GetValue(i)) != 0;
    }

    public static DateTime? DataHoraN(this MySqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetDateTime(i);
    }

    public static DateTime DataHora(this MySqlDataReader r, string col) => r.DataHoraN(col) ?? DateTime.MinValue;

    public static DateOnly? DataN(this MySqlDataReader r, string col) =>
        r.DataHoraN(col) is DateTime d ? DateOnly.FromDateTime(d) : null;

    public static DateOnly Data(this MySqlDataReader r, string col) => r.DataN(col) ?? DateOnly.MinValue;

    public static TimeOnly? HoraN(this MySqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : TimeOnly.FromTimeSpan(r.GetTimeSpan(i));
    }
}
