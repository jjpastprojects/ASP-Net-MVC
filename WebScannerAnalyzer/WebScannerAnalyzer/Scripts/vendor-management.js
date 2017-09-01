var vendorName = '';

$(document).ready(function () {

    $("#vendor_search_result_table")
        .on("preXhr.dt", function(e, settings, data) {
            $.LoadingOverlay("show");
        })
        .on("xhr.dt", function(e, settings, json, xhr) {
            $.LoadingOverlay("hide");
        })
        .DataTable({
        "scrollX": true,
        "bScrollCollapse": false,
        "bLengthChange": false,
        "bFilter": false,
        "autoWidth": false,
        "pagingType": "full_numbers",
        "serverSide": true,
        "processing": true,
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
            { "data": "VendorId", "name": "VendorId"},
            { "data": "VendorName", "name": "VendorName" },
            { "data": "ContactPerson", "name": "ContactPerson" },
            { "data": "Email", "name": "Email" },
            { "data": "DaysToFollow", "name": "DaysToFollow" },
            { "data": "IsDropShipper", "name": "IsDropShipper" },
            { "data": "Zip", "name": "Zip" },
            { "data": "StreetAddress", "name": "StreetAddress" },
            { "data": "OptionalAddress", "name": "OptionalAddress" },
            { "data": "City", "name": "City" },
            { "data": "State", "name": "State" },
            { "data": "Country", "name": "Country" },
            { "data": "CanBarcode", "name": "CanBarcode" },
            { "data": "FreeFreightProgram", "name": "FreeFreightProgram" },
            { "data": "DiscountProgram", "name": "DiscountProgram" },
            { "data": "WillUseAmazonUPSLabel", "name": "WillUseAmazonUPSLabel" },
            { "data": "AvgLeadTimeToShip", "name": "AvgLeadTimeToShip", "defaultContent": 0 },
            { "data": "AvgDaysInTransit", "name": "AvgDaysInTransit", "defaultContent": 0 },
            { "data": "AvgDaysToAcceptGoods", "name": "AvgDaysToAcceptGoods", "defaultContent": 0 },
            { "data": "TotalDaysFromOrderToSell", "name": "TotalDaysFromOrderToSell" }
        ],
        "columnDefs" :[
            { "orderable": false, "targets": 0, "visible": false },
            { "className": "text-center", "targets": [4, 5, 12, 13, 15, 16, 17, 18, 19] },
            {
                "targets": "_all",
                "createdCell": function (td, cellData, rowData, row, col) {
                    td.title = cellData;
                }
            }
        ],
        "ajax": {
            "url": siteRoot + "VendorManagement/Search",
            "type": "POST",
            "data": function(d) {
                d.vendorName = vendorName;
            },
            "error": function () {
                $.LoadingOverlay("hide");
                showDialog("Search Vendor fail", "<span class='error-text'>Error while processing your request. Please try again later.</span>");
            }
        },
        "order": [[1, "asc"]]
    });

    $("#search_button").click(function () {
        vendorName = $("#vendor_name").val();
        $("#vendor_search_result_table").DataTable().ajax.reload();
    });

    $("#vendor_search_result_table tbody").on("dblclick", "tr", function() {
        var table = $("#vendor_search_result_table").DataTable();
        var data = table.row(this).data();
        var vendorId = data["VendorId"];

        window.location.href = siteRoot + "VendorManagement/Edit/" + vendorId;
    });
});