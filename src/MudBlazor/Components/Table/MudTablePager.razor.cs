using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;

namespace MudBlazor
{
    public partial class MudTablePager : MudComponentBase
    {
        protected string Classname =>
                    new CssBuilder("mud-table-pagination-toolbar")
                    .AddClass("mud-tablepager-left", !RightToLeft)
                    .AddClass("mud-tablepager-right", RightToLeft)
                    .AddClass(Class)
                    .Build();

        protected string PaginationClassname =>
            new CssBuilder("mud-table-pagination-display")
            .AddClass("mud-tablepager-left", !RightToLeft)
            .AddClass("mud-tablepager-right", RightToLeft)
            .AddClass(Class)
            .Build();

        [CascadingParameter(Name = "RightToLeft")] public bool RightToLeft { get; set; }

        [CascadingParameter] public TableContext Context { get; set; }

        /// <summary>
        /// Set true to hide the part of the pager which allows to change the page size.
        /// </summary>
        [Parameter] public bool HideRowsPerPage { get; set; }

        /// <summary>
        /// Set true to hide the part of the pager which allows to change the page size.
        /// </summary>
        [ExcludeFromCodeCoverage]
        [Obsolete("Use HideRowsPerPage instead.", true)]
        [Parameter] public bool DisableRowsPerPage { get => HideRowsPerPage; set => HideRowsPerPage = value; }

        /// <summary>
        /// Set true to hide the number of pages.
        /// </summary>
        [Parameter] public bool HidePageNumber { get; set; }

        /// <summary>
        /// Set true to hide the pagination.
        /// </summary>
        [Parameter] public bool HidePagination { get; set; }

        /// <summary>
        /// Set the horizontal alignment position.
        /// </summary>
        [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Right;

        /// <summary>
        /// Sentinel page size value representing the "Auto" option: the hosting table computes the
        /// rows-per-page from the available container height. This value never reaches
        /// <see cref="MudTableBase.SetRowsPerPage(int)"/> — selecting it only fires <see cref="OnAutoSelected"/>.
        /// </summary>
        public const int AutoPageSize = -1;

        /// <summary>
        /// Define a list of available page size options for the user to choose from
        /// </summary>
        [Parameter] public int[] PageSizeOptions { get; set; } = new int[] { 10, 25, 50, 100 };

        /// <summary>
        /// Format string for the display of the current page, which you can localize to your language. Available variables are:
        /// {first_item}, {last_item} and {all_items} which will replaced with the indices of the page's first and last item, as well as the total number of items.
        /// Default: "{first_item}-{last_item} of {all_items}" which is transformed into "0-25 of 77". 
        /// </summary>
        [Parameter] public string InfoFormat { get; set; } = "{first_item}-{last_item} of {all_items}";

        /// <summary>
        /// Defines the text shown in the items per page dropdown when a user provides int.MaxValue as an option
        /// </summary>
        [Parameter] public string AllItemsText { get; set; } = "All";

        /// <summary>
        /// Defines the text shown in the items per page dropdown when a user provides <see cref="AutoPageSize"/> as an option.
        /// </summary>
        [Parameter] public string AutoItemsText { get; set; } = "Auto";

        /// <summary>
        /// Optional shorter text shown in the CLOSED page-size select when the Auto option is selected
        /// (the open dropdown keeps showing <see cref="AutoItemsText"/>). When null, <see cref="AutoItemsText"/>
        /// is shown in both places.
        /// </summary>
        [Parameter] public string AutoSelectedText { get; set; }

        /// <summary>
        /// When true, the page-size dropdown displays the <see cref="AutoItemsText"/> item as selected instead of the
        /// table's current (auto-computed) RowsPerPage. Bind this to the hosting container's auto-rows-per-page mode flag.
        /// </summary>
        [Parameter] public bool AutoRowsPerPageSelected { get; set; }

        /// <summary>
        /// Fires when the user picks the <see cref="AutoPageSize"/> option. The sentinel is never forwarded to the table.
        /// </summary>
        [Parameter] public EventCallback OnAutoSelected { get; set; }

        /// <summary>
        /// Fires on every explicit concrete (non-Auto) page-size pick, BEFORE the size is forwarded to the table —
        /// even when the picked value equals the table's current value (which MudTableBase's same-value guard would
        /// otherwise swallow). Lets a container disable its auto mode synchronously before the RowsPerPageChanged echo.
        /// </summary>
        [Parameter] public EventCallback<int> OnConcretePageSizeSelected { get; set; }

        private string Info
        {
            get
            {
                // fetch number of filtered items (once only)
                var filteredItemsCount = Table?.GetFilteredItemsCount() ?? 0;

                return Table == null
                    ? "Table==null"
                    : InfoFormat
                        .Replace("{first_item}", $"{(filteredItemsCount == 0 ? 0 : Table?.CurrentPage * Table.RowsPerPage + 1)}")
                        .Replace("{last_item}", $"{Math.Min((Table.CurrentPage + 1) * Table.RowsPerPage, filteredItemsCount)}")
                        .Replace("{all_items}", $"{filteredItemsCount}");
            }
        }

        /// <summary>
        /// The localizable "Rows per page:" text.
        /// </summary>
        [Parameter] public string RowsPerPageString { get; set; } = "Rows per page:";

        /// <summary>
        /// Custom first icon.
        /// </summary>
        [Parameter] public string FirstIcon { get; set; } = Icons.Material.Filled.FirstPage;

        /// <summary>
        /// Custom before icon.
        /// </summary>
        [Parameter] public string BeforeIcon { get; set; } = Icons.Material.Filled.NavigateBefore;

        /// <summary>
        /// Custom next icon.
        /// </summary>
        [Parameter] public string NextIcon { get; set; } = Icons.Material.Filled.NavigateNext;

        /// <summary>
        /// Custom last icon.
        /// </summary>
        [Parameter] public string LastIcon { get; set; } = Icons.Material.Filled.LastPage;

        private async Task SetRowsPerPageAsync(int size)
        {
            if (size == AutoPageSize)
            {
                // The sentinel must never reach MudTableBase (it would be treated as a real page size).
                if (OnAutoSelected.HasDelegate)
                    await OnAutoSelected.InvokeAsync();
                return;
            }
            // Awaited BEFORE forwarding the size, so a hosting container can flip its auto mode off
            // synchronously before Table.SetRowsPerPage fires the RowsPerPageChanged echo.
            if (OnConcretePageSizeSelected.HasDelegate)
                await OnConcretePageSizeSelected.InvokeAsync(size);
            Table?.SetRowsPerPage(size);
        }

        private bool BackButtonsDisabled => Table == null ? false : Table.CurrentPage == 0;

        private bool ForwardButtonsDisabled => Table == null ? false : (Table.CurrentPage + 1) * Table.RowsPerPage >= Table.GetFilteredItemsCount();

        public MudTableBase Table => Context?.Table;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (Context != null)
            {
                Context.HasPager = true;
                Context.PagerStateHasChanged = StateHasChanged;
                // Skip the Auto sentinel when defaulting, and set the size on the table directly —
                // routing through SetRowsPerPageAsync here would fire OnConcretePageSizeSelected on
                // every init and disable a restored auto-rows-per-page mode.
                var size = Table._rowsPerPage ?? PageSizeOptions.FirstOrDefault(o => o != AutoPageSize);
                Table?.SetRowsPerPage(size);
            }
        }

    }
}
