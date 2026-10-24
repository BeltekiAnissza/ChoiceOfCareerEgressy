// Responsive tables: automatically convert regular data tables to cards on small screens.
// Does not clone/move form elements, so ASP.NET antiforgery tokens and handlers remain intact.
(function () {
  function initialize() {
    document.querySelectorAll('main table.table, .app-main table.table').forEach(function (table) {
      if (table.classList.contains('no-mobile-cards') || table.closest('.no-mobile-cards')) return;
      var head = table.tHead;
      var body = table.tBodies[0];
      if (!head || !body || head.rows.length !== 1) return;
      var headers = Array.from(head.rows[0].cells);
      if (!headers.length || headers.some(function (h) { return h.colSpan !== 1 || h.rowSpan !== 1; })) return;
      var rows = Array.from(body.rows);
      if (rows.some(function (r) { return r.cells.length !== headers.length || Array.from(r.cells).some(function (c) { return c.colSpan !== 1 || c.rowSpan !== 1; }); })) return;
      rows.forEach(function (row) {
        Array.from(row.cells).forEach(function (cell, i) {
          cell.setAttribute('data-label', headers[i].textContent.trim());
        });
      });
      table.classList.add('mobile-card-table', 'is-card-ready');
      // Bootstrap's .table-responsive sets overflow-x:auto; avoid card clipping.
      var wrapper = table.closest('.table-responsive');
      if (wrapper) wrapper.classList.add('mobile-cards-wrapper');
    });
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize);
  else initialize();
})();
