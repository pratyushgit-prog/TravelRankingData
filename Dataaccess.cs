using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace TravelRankingData
{
    /// <summary>
    /// DataAccess with fast-fail connection handling.
    /// Overrides Connect Timeout to 8s so the page never hangs on a dead DB.
    /// </summary>
    public class DataAccess
    {
        private readonly string _connectionString;

        // Maximum seconds to WAIT FOR A CONNECTION to open.
        // Web.config has 500 — we override to 8 so the page fails fast instead of hanging.
        private const int ConnectTimeoutSeconds = 8;

        // Maximum seconds to wait for a QUERY to complete once connected.
        private const int CommandTimeoutSeconds = 30;

        public DataAccess()
        {
            string raw = ConfigurationManager.ConnectionStrings["SkyDataConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(raw))
                throw new Exception("Connection string 'SkyDataConnection' not found in Web.config");

            // Override Connect Timeout so we fail fast instead of hanging for minutes
            var builder = new SqlConnectionStringBuilder(raw)
            {
                ConnectTimeout = ConnectTimeoutSeconds
            };
            _connectionString = builder.ConnectionString;
        }

        // ── Connection test ───────────────────────────────────────────────
        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        // ── Main data retrieval ───────────────────────────────────────────
        public DataTable GetTravelRankingsData()
        {
            return ExecuteQuery(@"
                SELECT
                    [Id],
                    [Origin],
                    [Dest],
                    [Cabin_Class],
                    [AirLine],
                    [Adult],
                    [Child],
                    [Infant],
                    [Obdate],
                    [Ibdate],
                    [Insertdate]
                FROM [SKYDATA].[dbo].[TravelRankings_data]
                ORDER BY [Insertdate] DESC
            ");
        }

        // ── Distinct cabin classes for dropdown ───────────────────────────
        public DataTable GetDistinctCabinClasses()
        {
            return ExecuteQuery(@"
                SELECT DISTINCT [Cabin_Class]
                FROM [SKYDATA].[dbo].[TravelRankings_data]
                WHERE [Cabin_Class] IS NOT NULL
                ORDER BY [Cabin_Class]
            ");
        }

        // ── Distinct origins ──────────────────────────────────────────────
        public DataTable GetDistinctOrigins()
        {
            return ExecuteQuery(@"
                SELECT DISTINCT [Origin]
                FROM [SKYDATA].[dbo].[TravelRankings_data]
                WHERE [Origin] IS NOT NULL
                ORDER BY [Origin]
            ");
        }

        // ── Distinct destinations ─────────────────────────────────────────
        public DataTable GetDistinctDestinations()
        {
            return ExecuteQuery(@"
                SELECT DISTINCT [Dest]
                FROM [SKYDATA].[dbo].[TravelRankings_data]
                WHERE [Dest] IS NOT NULL
                ORDER BY [Dest]
            ");
        }

        // ── Distinct airlines ─────────────────────────────────────────────
        public DataTable GetDistinctAirlines()
        {
            return ExecuteQuery(@"
                SELECT DISTINCT [AirLine]
                FROM [SKYDATA].[dbo].[TravelRankings_data]
                WHERE [AirLine] IS NOT NULL
                ORDER BY [AirLine]
            ");
        }

        // ── Total record count ────────────────────────────────────────────
        public int GetTotalRecordCount()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM [SKYDATA].[dbo].[TravelRankings_data]", con))
                {
                    cmd.CommandTimeout = CommandTimeoutSeconds;
                    con.Open();
                    return (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting record count: " + ex.Message, ex);
            }
        }

        // ── Paged retrieval ───────────────────────────────────────────────
        public DataTable GetTravelRankingsDataPaged(int pageNumber, int pageSize)
        {
            int offset = (pageNumber - 1) * pageSize;
            string sql = string.Format(@"
                SELECT [Id],[Origin],[Dest],[Cabin_Class],[AirLine],
                       [Adult],[Child],[Infant],[Obdate],[Ibdate],[Insertdate]
                FROM [SKYDATA].[dbo].[TravelRankings_data]
                ORDER BY [Insertdate] DESC
                OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY
            ", offset, pageSize);

            return ExecuteQuery(sql);
        }

        // ── Summary stats ─────────────────────────────────────────────────
        public Dictionary<string, object> GetDataSummary()
        {
            var summary = new Dictionary<string, object>();
            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
                    SELECT
                        COUNT(*)                  AS TotalRecords,
                        COUNT(DISTINCT [Origin])  AS OriginCount,
                        COUNT(DISTINCT [Dest])    AS DestinationCount,
                        COUNT(DISTINCT [AirLine]) AS AirlineCount,
                        SUM([Adult])              AS TotalAdults,
                        SUM([Child])              AS TotalChildren,
                        SUM([Infant])             AS TotalInfants,
                        MIN([Insertdate])         AS OldestRecord,
                        MAX([Insertdate])         AS LatestRecord
                    FROM [SKYDATA].[dbo].[TravelRankings_data]
                ", con))
                {
                    cmd.CommandTimeout = CommandTimeoutSeconds;
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            summary["TotalRecords"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            summary["OriginCount"] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            summary["DestinationCount"] = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            summary["AirlineCount"] = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                            summary["TotalAdults"] = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                            summary["TotalChildren"] = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                            summary["TotalInfants"] = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
                            summary["OldestRecord"] = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7);
                            summary["LatestRecord"] = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting data summary: " + ex.Message, ex);
            }
            return summary;
        }

        // ── Private helper: run any SELECT and return a DataTable ─────────
        private DataTable ExecuteQuery(string sql, Action<SqlCommand> addParams = null)
        {
            var dt = new DataTable();
            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = CommandTimeoutSeconds;
                    addParams?.Invoke(cmd);

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                throw new Exception("Database error: " + sqlEx.Message, sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception("Query error: " + ex.Message, ex);
            }
            return dt;
        }
    }
}