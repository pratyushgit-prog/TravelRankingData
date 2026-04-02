<%@ Page Title="Travel Rankings" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TravelRankingData._Default" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

<style>
    :root {
        --navy: #0f1b2d;
        --sky: #1e90ff;
        --sky-light: #e8f4ff;
        --surface: #ffffff;
        --border: #d1dce8;
        --text: #1a2533;
        --muted: #6b7c93;
        --radius: 8px;
    }

    /* ── Filter card ── */
    .tr-filter-card {
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius);
        padding: 20px 24px 16px;
        margin-bottom: 20px;
        box-shadow: 0 1px 4px rgba(0,0,0,.06);
    }
    .tr-filter-card h5 {
        font-size: .8rem;
        font-weight: 700;
        letter-spacing: .08em;
        text-transform: uppercase;
        color: var(--muted);
        margin-bottom: 14px;
    }
    .tr-filter-row { display: flex; flex-wrap: wrap; gap: 10px; align-items: flex-end; }
    .tr-filter-group { display: flex; flex-direction: column; gap: 4px; }
    .tr-filter-group label { font-size: .75rem; font-weight: 600; color: var(--muted); }
    .tr-filter-group input,
    .tr-filter-group select {
        border: 1px solid var(--border);
        border-radius: 6px;
        padding: 7px 10px;
        font-size: .85rem;
        min-width: 120px;
        background: #fff;
        transition: border-color .15s;
    }
    .tr-filter-group input:focus,
    .tr-filter-group select:focus {
        border-color: var(--sky);
        outline: none;
        box-shadow: 0 0 0 3px rgba(30,144,255,.15);
    }
    .tr-btn { padding: 8px 18px; border-radius: 6px; border: none; font-size: .83rem; font-weight: 600; cursor: pointer; transition: all .15s; }
    .tr-btn-primary   { background: var(--sky); color: #fff; }
    .tr-btn-primary:hover   { background: #1270d8; }
    .tr-btn-secondary { background: #fff; color: var(--muted); border: 1px solid var(--border); }
    .tr-btn-secondary:hover { background: #f5f8fb; }
    .tr-btn-outline   { background: #fff; color: var(--sky); border: 1px solid var(--sky); }
    .tr-btn-outline:hover   { background: var(--sky-light); }

    /* ── Grid card ── */
    .tr-grid-card {
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius);
        box-shadow: 0 1px 4px rgba(0,0,0,.06);
        overflow: hidden;
    }
    .tr-grid-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 14px 20px;
        border-bottom: 1px solid var(--border);
        background: #fafbfd;
    }
    .tr-grid-header h4 { font-size: .95rem; font-weight: 700; margin: 0; }
    .tr-page-info { font-size: .78rem; color: var(--muted); }

    /* ── Table ── */
    .tr-table { width: 100%; border-collapse: collapse; font-size: .82rem; }
    .tr-table thead th {
        background: var(--navy);
        color: #fff;
        padding: 10px 12px;
        text-align: left;
        font-size: .72rem;
        font-weight: 700;
        letter-spacing: .05em;
        text-transform: uppercase;
        white-space: nowrap;
        cursor: pointer;
        user-select: none;
    }
    .tr-table thead th:hover { background: #1a2e48; }
    .tr-table thead th a,
    .tr-table thead th a:visited,
    .tr-table thead th a:hover { color: #fff !important; text-decoration: none; }
    .tr-table tbody tr { border-bottom: 1px solid #eef2f7; transition: background .1s; }
    .tr-table tbody tr:hover { background: var(--sky-light); }
    .tr-table tbody td { padding: 9px 12px; vertical-align: middle; }
    .tr-table tbody tr:nth-child(even) { background: #fafbfd; }
    .tr-table tbody tr:nth-child(even):hover { background: var(--sky-light); }

    /* ── Badges ── */
    .b    { display: inline-block; padding: 2px 8px; border-radius: 20px; font-size: .72rem; font-weight: 700; letter-spacing: .03em; }
    .b-or { background: #e0f0ff; color: #1565c0; }
    .b-de { background: #e8f5e9; color: #2e7d32; }
    .b-ca { background: #fff3e0; color: #e65100; }

    /* ── Pager ── */
    .tr-pager { padding: 10px 20px; border-top: 1px solid var(--border); background: #fafbfd; text-align: center; }
    .tr-pager table { margin: 0 auto; }
    .tr-pager table td a {
        display: inline-block; padding: 5px 10px; border-radius: 5px;
        font-size: .8rem; font-weight: 600; text-decoration: none; color: var(--sky);
        border: 1px solid var(--border); margin: 0 2px;
    }
    .tr-pager table td a:hover { background: var(--sky-light); }
    .tr-pager table td span {
        display: inline-block; padding: 5px 10px; border-radius: 5px;
        font-size: .8rem; font-weight: 600; background: var(--sky); color: #fff; margin: 0 2px;
    }

    /* ── Error ── */
    .tr-error         { display: none; background: #fff0f0; border: 1px solid #ffcccc; color: #c0392b; border-radius: 6px; padding: 10px 16px; margin-bottom: 14px; font-size: .85rem; }
    .tr-error.visible { display: block; }

    /* ── Null / empty values ── */
    .tr-null { color: #bbb; font-style: italic; font-size: .78rem; }
</style>

    <asp:Label ID="lblError" runat="server" CssClass="tr-error" Visible="false"></asp:Label>

    <%-- ── Filter card ── --%>
    <div class="tr-filter-card">
        <h5>🔍 Filter Records</h5>
        <div class="tr-filter-row">

            <div class="tr-filter-group">
                <label>Origin</label>
                <asp:TextBox ID="txtOrigin" runat="server" placeholder="e.g. LON" />
            </div>

            <div class="tr-filter-group">
                <label>Destination</label>
                <asp:TextBox ID="txtDest" runat="server" placeholder="e.g. BKK" />
            </div>

            <div class="tr-filter-group">
                <label>Airline</label>
                <asp:TextBox ID="txtAirline" runat="server" placeholder="e.g. BA" />
            </div>

            <div class="tr-filter-group">
                <label>Cabin Class</label>
                <asp:DropDownList ID="ddlCabin" runat="server" />
            </div>

            <div class="tr-filter-group">
                <label>Outbound From</label>
                <asp:TextBox ID="txtObDateFrom" runat="server" TextMode="Date" />
            </div>

            <div class="tr-filter-group">
                <label>Outbound To</label>
                <asp:TextBox ID="txtObDateTo" runat="server" TextMode="Date" />
            </div>

            <div class="tr-filter-group">
                <label>Insert Date From</label>
                <asp:TextBox ID="txtInsertFrom" runat="server" TextMode="Date" />
            </div>

            <div class="tr-filter-group">
                <label>Insert Date To</label>
                <asp:TextBox ID="txtInsertTo" runat="server" TextMode="Date" />
            </div>

            <div class="tr-filter-group" style="flex-direction:row; gap:8px; padding-top:2px;">
                <asp:Button ID="btnSearch"  runat="server" Text="Search"  CssClass="tr-btn tr-btn-primary"   OnClick="btnSearch_Click" />
                <asp:Button ID="btnClear"   runat="server" Text="Clear"   CssClass="tr-btn tr-btn-secondary" OnClick="btnClear_Click" />
                <asp:Button ID="btnRefresh" runat="server" Text="Refresh" CssClass="tr-btn tr-btn-outline"   OnClick="btnRefresh_Click" />
            </div>

        </div>
    </div>

    <%-- ── Grid card ── --%>
    <div class="tr-grid-card">
        <div class="tr-grid-header">
            <h4>✈ Travel Rankings Data</h4>
            <span class="tr-page-info">
                <asp:Label ID="lblPageInfo" runat="server" />
            </span>
        </div>

        <asp:GridView
            ID="gvTravelRankings"
            runat="server"
            AutoGenerateColumns="false"
            AllowPaging="true"
            PageSize="25"
            AllowSorting="true"
            CssClass="tr-table"
            OnSorting="gvTravelRankings_Sorting"
            OnPageIndexChanging="gvTravelRankings_PageIndexChanging"
            OnRowDataBound="gvTravelRankings_RowDataBound"
            GridLines="None"
            PagerStyle-CssClass="tr-pager">

            <PagerSettings Mode="NumericFirstLast" PageButtonCount="10" />

            <EmptyDataTemplate>
                <div style="padding:40px; text-align:center; color:#888; font-size:.88rem;">
                    No records match the current filters.
                </div>
            </EmptyDataTemplate>

            <Columns>
                <asp:BoundField DataField="Id"          HeaderText="ID"       SortExpression="Id"         ItemStyle-Width="55px" />
                <asp:BoundField DataField="Origin"      HeaderText="Origin"   SortExpression="Origin" />
                <asp:BoundField DataField="Dest"        HeaderText="Dest"     SortExpression="Dest" />
                <asp:BoundField DataField="Cabin_Class" HeaderText="Cabin"    SortExpression="Cabin_Class" />
                <asp:BoundField DataField="AirLine"     HeaderText="Airline"  SortExpression="AirLine" />
                <asp:BoundField DataField="Adult"       HeaderText="Adults"   SortExpression="Adult"      ItemStyle-Width="60px" />
                <asp:BoundField DataField="Child"       HeaderText="Children" SortExpression="Child"      ItemStyle-Width="70px" />
                <asp:BoundField DataField="Infant"      HeaderText="Infants"  SortExpression="Infant"     ItemStyle-Width="65px" />
                <asp:BoundField DataField="Obdate"      HeaderText="Outbound" SortExpression="Obdate"     DataFormatString="{0:dd MMM yyyy}" HtmlEncode="false" />
                <asp:BoundField DataField="Ibdate"      HeaderText="Inbound"  SortExpression="Ibdate"     DataFormatString="{0:dd MMM yyyy}" HtmlEncode="false" />
                <asp:BoundField DataField="Insertdate"  HeaderText="Inserted" SortExpression="Insertdate" DataFormatString="{0:dd MMM yyyy HH:mm}" HtmlEncode="false" />
            </Columns>
        </asp:GridView>
    </div>

</asp:Content>
