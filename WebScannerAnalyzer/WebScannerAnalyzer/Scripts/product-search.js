$(document).ready(function () {

    $("#product_search_result_table").DataTable(
    {
        "scrollX": true,
        "bScrollCollapse": false,
        "bLengthChange": false,
        "bFilter": false,
        "autoWidth": false,
        "pagingType": "full_numbers",
        "oLanguage": {
            "oPaginate": {
                "sNext": ">",
                "sPrevious": "<",
                "sFirst": "<<",
                "sLast": ">>"
            },
            "sEmptyTable": "No records to display"
        },
        "columns": [
            { "data": "Asin", "name": "Asin", "render": function(data, type, full, meta) {
                return "<a target='_blank' href='https://www.amazon.com/dp/" + data + "'>" + data + "</a>";
            } },
            { "data": "Sku", "name": "Sku" },
            { "data": "AverageRanking", "name": "AverageRanking" },
            { "data": "QuantityInStock", "name": "QuantityInStock" },
            { "data": "CasePack", "name": "CasePack" },
            { "data": "FirstCategory", "name": "FirstCategory" },
            { "data": "FirstCategoryRank", "name": "FirstCategoryRank" },
            { "data": "Likes", "name": "Likes" },
            { "data": "Cost", "name": "Cost" },
            { "data": "SalesValue", "name": "SalesValue" },
            { "data": "Profit", "name": "Profit" },
            { "data": "BBPrice", "name": "BBPrice" },
            { "data": "BBShipping", "name": "BBShipping" },
            { "data": "BBDelivered", "name": "BBDelivered" },
            { "data": "Seller", "name": "Seller" },
            { "data": "Name", "name": "Name" },
            { "data": "AmazonShippingWeight", "name": "AmazonShippingWeight" },
            { "data": "ShippingWeight", "name": "ShippingWeight" },
            { "data": "Manufacturer", "name": "Manufacturer" }
        ],
        "columnDefs": [
            { "className": "text-center", "targets": [2, 3, 4, 7, 8, 9, 10, 11, 12, 13, 16, 17] },
            {
                "targets": "_all",
                "createdCell": function (td, cellData, rowData, row, col) {
                    td.title = cellData;
                }
            }
        ],
        "order": [[6, "asc"]]
    });

    $("#search_button").click(function () {
        $.LoadingOverlay("show");
        $.ajax({
            url: siteRoot + "Product/Search",
            type: "POST",
            cache: false,
            data: {
                VendorName: $("#vendor_dropdown option:selected").text(),
                Description: $("#description_textbox").val(),
                QuantityInStock: $("#quantity_textbox").val(),
                Sku: $("#sku_textbox").val(),
                CostToBringOver: $("#cost_textbox").val(),
                IsWatchList: $("#watch_list_checkbox").val(),
                LimitRecords: $("#limit_records_textbox").val(),
                MinProfit: $("#min_profit_textbox").val(),
                MinProfitPecent: $("#min_profit_percent_textbox").val(),
                Discount: $("#discount_textbox").val()
            },
            success: function(data) {
                var tbl = $("#product_search_result_table").dataTable();
                tbl.fnClearTable();

                if (data.length > 0)
                    tbl.fnAddData(data);

                $.LoadingOverlay("hide", true);
            },
            error: function (xhr, status, error) {
                showDialog("Search Product fail", "<span class='error-text'>Error while processing your request. Please try again later.</span>");
                $.LoadingOverlay("hide", true);
            }
        });
    });
});