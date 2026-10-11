namespace RatioMaster.App.Views.Controls
{
    using System;
    using System.Linq;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Primitives;
    using Avalonia.VisualTree;

    /// <summary>
    /// Widens grid columns whose header does not fit. The default widths suit the English headers; many translations
    /// are longer, and a header cut short is hard to read even with its ellipsis and tooltip.
    /// </summary>
    public static class DataGridHeaders
    {
        /// <summary>
        /// Measures every header the grid has created and widens its column to fit it. Columns for which
        /// <paramref name="mayWiden"/> returns false keep their width, and a column is never made narrower.
        /// </summary>
        public static void Fit(DataGrid grid, Func<DataGridColumn, bool> mayWiden)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(mayWiden);
            foreach (var header in grid.GetVisualDescendants().OfType<DataGridColumnHeader>())
            {
                var column = grid.Columns.FirstOrDefault(c => c.Header is not null && Equals(c.Header, header.Content));
                if (column is null || !column.Width.IsAbsolute || !mayWiden(column))
                {
                    continue;
                }

                header.Measure(Size.Infinity);
                var needed = Math.Ceiling(header.DesiredSize.Width) + 2;
                header.InvalidateMeasure();
                if (needed > column.Width.Value)
                {
                    column.Width = new DataGridLength(needed);
                }
            }
        }
    }
}
