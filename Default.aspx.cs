using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace TravelRankingData
{
    public partial class _Default : Page
    {
        private DataAccess _dataAccess;

        private const string VS_SORT_COL = "SortCol";
        private const string VS_SORT_DIR = "SortDir";

        private string SortColumn
        {
            get { return ViewState[VS_SORT_COL] as string ?? "Insertdate"; }
            set { ViewState[VS_SORT_COL] = value; }
        }

        private string SortDirection
        {
            get { return ViewState[VS_SORT_DIR] as string ?? "DESC"; }
            set { ViewState[VS_SORT_DIR] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            _dataAccess = new DataAccess();

            if (!IsPostBack)
            {
                LoadCabinClasses();
                BindGridData();
            }
        }

        // ── Cabin class dropdown ───────────────────────────────────────────
        private void LoadCabinClasses()
        {
            ddlCabin.Items.Clear();
            ddlCabin.Items.Add(new ListItem("All Cabins", ""));

            try
            {
                DataTable dt = _dataAccess.GetDistinctCabinClasses();
                foreach (DataRow row in dt.Rows)
                {
                    string cabin = row[0]?.ToString();
                    if (!string.IsNullOrWhiteSpace(cabin))
                        ddlCabin.Items.Add(new ListItem(cabin, cabin));
                }
            }
            catch { /* leave as "All Cabins" only if DB unreachable */ }
        }

        // ── Bind grid ──────────────────────────────────────────────────────
        private void BindGridData()
        {
            HideError();

            try
            {
                // Read filter values
                string origin = txtOrigin.Text.Trim();
                string dest = txtDest.Text.Trim();
                string airline = txtAirline.Text.Trim();
                string cabin = ddlCabin.SelectedValue;
                DateTime? obFrom = ParseDate(txtObDateFrom.Text);
                DateTime? obTo = ParseDate(txtObDateTo.Text);
                DateTime? insFrom = ParseDate(txtInsertFrom.Text);
                DateTime? insTo = ParseDate(txtInsertTo.Text);

                // Fetch data
                DataTable dt = _dataAccess.GetTravelRankingsData();

                // In-memory filter
                bool hasFilter = !string.IsNullOrEmpty(origin + dest + airline + cabin)
                              || obFrom.HasValue || obTo.HasValue
                              || insFrom.HasValue || insTo.HasValue;

                if (hasFilter)
                    dt = FilterTable(dt, origin, dest, airline, cabin, obFrom, obTo, insFrom, insTo);

                // Sort
                dt.DefaultView.Sort = SortColumn + " " + SortDirection;
                DataTable sorted = dt.DefaultView.ToTable();

                gvTravelRankings.DataSource = sorted;
                gvTravelRankings.DataBind();

                int total = sorted.Rows.Count;
                int pages = (int)Math.Ceiling((double)total / gvTravelRankings.PageSize);
                int cur = gvTravelRankings.PageIndex + 1;

                lblPageInfo.Text = string.Format(
                    "{0} record{1} &nbsp;|&nbsp; Page {2} of {3}",
                    total, total == 1 ? "" : "s", cur, Math.Max(pages, 1));
            }
            catch (Exception ex)
            {
                // Show a clear error — never leave the user staring at a blank/loading page
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                // Detect connection-timeout specifically
                if (msg.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("server was not found", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ShowError("⚠ Cannot reach the database server (10.17.128.3). " +
                              "Check that SQL Server is running and the firewall allows port 1433. " +
                              "Detail: " + msg);
                }
                else
                {
                    ShowError("⚠ Error loading data: " + msg);
                }

                // Bind an empty grid so the page still renders
                gvTravelRankings.DataSource = new DataTable();
                gvTravelRankings.DataBind();
                lblPageInfo.Text = "0 records";
            }
        }

        // ── In-memory filter ──────────────────────────────────────────────
        private DataTable FilterTable(
            DataTable dt,
            string origin, string dest, string airline, string cabin,
            DateTime? obFrom, DateTime? obTo,
            DateTime? insFrom, DateTime? insTo)
        {
            var filtered = dt.Clone();
            foreach (DataRow row in dt.Rows)
            {
                bool match =
                    (string.IsNullOrEmpty(origin) || CellContains(row["Origin"], origin)) &&
                    (string.IsNullOrEmpty(dest) || CellContains(row["Dest"], dest)) &&
                    (string.IsNullOrEmpty(airline) || CellContains(row["AirLine"], airline)) &&
                    (string.IsNullOrEmpty(cabin) || CellEquals(row["Cabin_Class"], cabin));

                if (match && (obFrom.HasValue || obTo.HasValue))
                {
                    if (row["Obdate"] == DBNull.Value)
                    {
                        match = false;
                    }
                    else
                    {
                        DateTime d = Convert.ToDateTime(row["Obdate"]).Date;
                        if (obFrom.HasValue && d < obFrom.Value.Date) match = false;
                        if (obTo.HasValue && d > obTo.Value.Date) match = false;
                    }
                }

                if (match && (insFrom.HasValue || insTo.HasValue))
                {
                    if (row["Insertdate"] == DBNull.Value)
                    {
                        match = false;
                    }
                    else
                    {
                        DateTime d = Convert.ToDateTime(row["Insertdate"]).Date;
                        if (insFrom.HasValue && d < insFrom.Value.Date) match = false;
                        if (insTo.HasValue && d > insTo.Value.Date) match = false;
                    }
                }

                if (match) filtered.ImportRow(row);
            }
            return filtered;
        }

        private bool CellContains(object cell, string filter)
            => (cell?.ToString() ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        private bool CellEquals(object cell, string value)
            => string.Equals(cell?.ToString(), value, StringComparison.OrdinalIgnoreCase);

        private DateTime? ParseDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return DateTime.TryParse(text, out DateTime d) ? d : (DateTime?)null;
        }

        // ── Button events ─────────────────────────────────────────────────
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            gvTravelRankings.PageIndex = 0;
            BindGridData();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtOrigin.Text = "";
            txtDest.Text = "";
            txtAirline.Text = "";
            txtObDateFrom.Text = "";
            txtObDateTo.Text = "";
            txtInsertFrom.Text = "";
            txtInsertTo.Text = "";
            ddlCabin.SelectedIndex = 0;
            SortColumn = "Insertdate";
            SortDirection = "DESC";
            gvTravelRankings.PageIndex = 0;
            BindGridData();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            gvTravelRankings.PageIndex = 0;
            BindGridData();
        }

        // ── Grid events ───────────────────────────────────────────────────
        protected void gvTravelRankings_Sorting(object sender, GridViewSortEventArgs e)
        {
            SortDirection = SortColumn == e.SortExpression
                ? (SortDirection == "ASC" ? "DESC" : "ASC")
                : "ASC";
            SortColumn = e.SortExpression;
            gvTravelRankings.PageIndex = 0;
            BindGridData();
        }

        protected void gvTravelRankings_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvTravelRankings.PageIndex = e.NewPageIndex;
            BindGridData();
        }

        protected void gvTravelRankings_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            WrapBadge(e.Row.Cells[1], "b b-or");  // Origin
            WrapBadge(e.Row.Cells[2], "b b-de");  // Dest
            WrapBadge(e.Row.Cells[3], "b b-ca");  // Cabin

            // Airline — show dash if NULL
            if (string.IsNullOrWhiteSpace(e.Row.Cells[4].Text) || e.Row.Cells[4].Text == "&nbsp;")
                e.Row.Cells[4].Text = "<span class='tr-null'>—</span>";

            // Inbound — show dash if NULL
            if (string.IsNullOrWhiteSpace(e.Row.Cells[9].Text) || e.Row.Cells[9].Text == "&nbsp;")
                e.Row.Cells[9].Text = "<span class='tr-null'>—</span>";
        }

        private void WrapBadge(TableCell cell, string cssClass)
        {
            string val = cell.Text;
            if (!string.IsNullOrWhiteSpace(val) && val != "&nbsp;")
                cell.Text = string.Format("<span class=\"{0}\">{1}</span>", cssClass, val);
        }

        // ── Error helpers ─────────────────────────────────────────────────
        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
            lblError.Attributes["class"] = "tr-error visible";
        }

        private void HideError()
        {
            lblError.Visible = false;
        }
    }
}