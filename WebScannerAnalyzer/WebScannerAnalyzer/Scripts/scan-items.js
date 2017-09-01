$(document).ready(function() {
    var table = $("#scan_items_result_table").DataTable({
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
            { "data": "InputBarCode", "name": "InputUPC" },
            { "data": "InputManufactureName", "name": "InputManufactureName" },
            { "data": "InputManufacturePart", "name": "InputManufacturePart" },
            { "data": "InputProductName", "name": "InputProductName" },
            { "data": "OutputBarCode", "name": "OutputUPC" },
            { "data": "OutputManufactureName", "name": "OutputManufactureName" },
            { "data": "OutputManufacturePart", "name": "OutputManufacturePart" },
            { "data": "OutputProductName", "name": "OutputProductName" },
            { "data": "Asin", "name": "ASIN" },
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
            { "data": "AmazoneShippingWeight", "name": "AmazonShippingWeight" },
            { "data": "ShippingWeight", "name": "ShippingWeight" }
        ],
        "columnDefs": [
            {
                "targets": 8,
                "render": function(data, type, row) {
                    return "<a target='_blank' href='" + row.AmazonLink + "'>" + data + "</a>";
                }
            },
            {
                "targets": "_all",
                "createdCell": function (td, cellData, rowData, row, col) {
                    td.title = cellData;
                }
            }
        ],
        "dom": 'lBfrtip',
        "buttons":[]
    });

    $("#input_file").on("click", function() {
        $(this).val("");
    });

    $("#input_file").on("change", function () {
        if ($(this).val() === '')
            return;

        //check file type before submit, only accept text file.
        if (window.FileReader && window.Blob) {
            var fileType = $(this)[0].files[0].type;
            if (fileType !== "text/plain") {
                showDialog("Wrong file type", "<span class='error-text'>File is not supported. Only text file accepted.</span>");
                $(this).val("");
                return;
            }
        }

        var formData = new FormData();
        formData.append("inputFile", $("#input_file")[0].files[0]);
        formData.append("marketPlace", $("#market_dropdown").val());

        $.LoadingOverlay("show");

        $.ajax({
            url: siteRoot + "ScanItems/Scan",
            data: formData,
            type: "POST",
            contentType: false,
            processData: false,
            cache: false,
            success: function(data) {
                var tbl = $("#scan_items_result_table").dataTable();
                tbl.fnClearTable();

                if (data.length > 0)
                    tbl.fnAddData(data);

                $.LoadingOverlay("hide", true);
            },
            error: function (xhr, status, error) {
                showDialog("Scan items fail", "<span class='error-text'>Error while processing your request. Please try again later.</span>");
                $.LoadingOverlay("hide", true);
            }
        });
    });

    new $.fn.dataTable.Buttons(table, {
        buttons: [
            {
                extend: "excel",
                text: "Export the scan items to file",
                className: "btn btn-success"
            }
        ]
    }).container().appendTo($("#buttons_container"));

    $(".dt-button").removeClass("dt-button");
    $(".dt-buttons").removeClass("dt-buttons");
});