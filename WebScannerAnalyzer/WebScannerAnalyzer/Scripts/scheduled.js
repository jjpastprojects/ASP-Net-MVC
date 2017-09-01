$(document).ready(function () {
    
    var table = $("#product_br_apply_result_table").DataTable(
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
                "sEmptyTable": "No records to display."
            },
            "columns": [
                {
                    "data": "asin", "name": "asin", "render": function (data, type, full, meta) {
                        return "<a target='_blank' href='https://www.amazon.com/dp/" + data + "'>" + data + "</a>";
                    }
                },
                {
                    "data": "SKU", "name": "SKU"},
                { "data": "itemDescription", "name": "itemDescription" },
                {
                    "data": "Price", "name": "Price", "render": function (data, type, full, meta) {
                        return '' + data + '';
                    }
                },
                { "data": "recommendedQuantity", "name": "recommendedQuantity" },
                { "data": "Quantity", "name": "Quantity" },
                { "data": "SQuantity", "name": "SQuantity" },
                { "data": "D30SQuantity", "name": "D30SQuantity" },
                { "data": "D7SQuantity", "name": "D7SQuantity" },
                { "data": "ShippingWeight", "name": "ShippingWeight" },
                { "data": "DSFee", "name": "DSFee" },
                { "data": "CasePack", "name": "CasePack" },
                { "data": "ActualWeight", "name": "ActualWeight" },
                { "data": "SalesValue", "name": "SalesValue" },
                { "data": "SellingPrice", "name": "SellingPrice" },
                { "data": "AmazonWeight", "name": "AmazonWeight" },
                { "data": "Profit", "name": "Profit" },
                { "data": "AmazonWeightBasedFee", "name": "AmazonWeightBasedFee" },
                { "data": "Manufacturer", "name": "Manufacturer" },
                { "data": "Category", "name": "Category" },
                { "data": "CategoryRanking", "name": "CategoryRanking" }
            ],
            "columnDefs": [
                { "className": "text-center", "targets": [3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20] },
                {
                    "targets": "_all",
                    "createdCell": function (td, cellData, rowData, row, col) {
                        td.title = cellData;
                    }
                }
            ],
            //"order": [[2, "asc"]]
        });

    $("#br_apply_button").click(function () {
        var market_place = $("#market_dropdown").val();
        if (market_place === null || market_place === "") {
            showDialog("Warning", "<span class='error-text'>Please select market place!</span>");
            return;
        }

        var vendor_name = $("#vendor_dropdown option:selected").text();
        if (vendor_name === null || vendor_name === "") {
            showDialog("Warning", "<span class='error-text'>Please select vendor!</span>");
            return;
        }

        $.LoadingOverlay("show");
        $.ajax({
            url: siteRoot + "Scheduled/BRApply",
            type: "POST",
            cache: false,
            data: {
                MarketPlace: $("#market_dropdown").val(),
                VendorName: $("#vendor_dropdown option:selected").text()
            },
            success: function (data) {
                br_apply_data = data[0];
                freight_cost = data[1];
                profit_cost = data[2];

                var tbl = $("#product_br_apply_result_table").dataTable();
                tbl.fnClearTable();

                if (br_apply_data.length > 0)
                    tbl.fnAddData(br_apply_data);

                setCostTextBoxes();

                $.LoadingOverlay("hide", true);
            },
            error: function (xhr, status, error) {
                showDialog("BR apply fail", "<span class='error-text'>Error while processing your request. Please try again later.</span>");
                $.LoadingOverlay("hide", true);
            }
        });
    });

    var br_apply_data = [];
    var freight_cost = 0;
    var profit_cost = 0;

    $("#asa_apply_button").click(function () {
        var market_place = $("#market_dropdown").val();
        if (market_place === null || market_place === "") {
            showDialog("Warning", "<span class='error-text'>Please select market place!</span>");
            return;
        }

        var vendor_name = $("#vendor_dropdown option:selected").text();
        if (vendor_name === null || vendor_name === "") {
            showDialog("Warning", "<span class='error-text'>Please select vendor!</span>");
            return;
        }

        $.LoadingOverlay("show");
        $.ajax({
            url: siteRoot + "Scheduled/ASAApply",
            type: "POST",
            cache: false,
            data: {
                MarketPlace: $("#market_dropdown").val(),
                VendorName: $("#vendor_dropdown option:selected").text()
            },
            success: function (data) {
                br_apply_data = data[0];
                freight_cost = data[1];
                profit_cost = data[2];

                var tbl = $("#product_br_apply_result_table").dataTable();
                tbl.fnClearTable();

                if (br_apply_data.length > 0)
                    tbl.fnAddData(br_apply_data);

                setCostTextBoxes();

                $.LoadingOverlay("hide", true);
            },
            error: function (xhr, status, error) {
                showDialog("BR apply fail", "<span class='error-text'>Error while processing your request. Please try again later.</span>");
                $.LoadingOverlay("hide", true);
            }
        });
    });


    function setCostTextBoxes() {
        $("#freight_cost_textbox").val(freight_cost);

        var labeling_cost = parseFloat($("#labeling_cost_textbox").val());
        var prep_cost = parseFloat($("#prep_cost_textbox").val());

        var net_profit = profit_cost - labeling_cost - prep_cost;
        $("#net_profit_textbox").val(net_profit);
    }

    $("#labeling_cost_textbox").on('change', function () {
        setCostTextBoxes();
    });

    $("#prep_cost_textbox").on('change', function () {
        setCostTextBoxes();
    });

    //*
    $("#download_listorders_button").click( function () {
        var market_place = $("#market_dropdown").val();
        if (market_place === null || market_place === "") {
            showDialog("Warning", "<span class='error-text'>Please select market place!</span>");
            return;
        }

        var vendor_name = $("#vendor_dropdown option:selected").text();
        if (vendor_name === null || vendor_name === "") {
            showDialog("Warning", "<span class='error-text'>Please select vendor!</span>");
            return;
        }

        //showDialog("Download orders from Amazon", "<span class='error-text'>It will take some time. After downloading orders from Amazon, you can do BR Apply.</span>");
        
        
        $.LoadingOverlay("show");
        $.ajax({
            url: siteRoot + "Scheduled/DownloadOrders",
            type: "POST",
            cache: false,
            data: {
                MarketPlace: $("#market_dropdown").val(),
                VendorName: $("#vendor_dropdown option:selected").text()
            },
            success: function (data) {
                showDialog("Download orders from Amazon", "<span class='error-text'>Successfully downloaded. You can do BR Apply now.</span>");
                window.location.href = siteRoot + "Scheduled/Index";
                $.LoadingOverlay("hide", true);
            },
            error: function (xhr, status, error) {
                showDialog("Download fail", "<span class='error-text'>Error while processing your request. Please try again later.</span>");
                $.LoadingOverlay("hide", true);
            }
        });
    });//*/

    /*
    $("#product_br_apply_result_table tbody").on("dblclick", "tr", function () {
        var table = $("#product_br_apply_result_table").DataTable();
        var data = table.row(this).data();
        var itemCode = data["ItemCode"];
        var manufacturer = data["Manufacturer"];

        $.LoadingOverlay("show");
        $.ajax({
            url: siteRoot + "Scheduled/DeleteInventory",
            type: "POST",
            cache: false,
            data: {
                MarketPlace: $("#market_dropdown").val(),
                VendorName: $("#vendor_dropdown option:selected").text(),
                ItemCode: itemCode,
                Manufacturer: manufacturer
            },
            success: function (data) {
                showDialog("Delete Success", "<span class='error-text'>" + itemCode + "(" + manufacturer + ")" + " was deleted successfully.</span>");

                br_apply_data = data[0];
                freight_cost = data[1];
                profit_cost = data[2];

                var tbl = $("#product_br_apply_result_table").dataTable();
                tbl.fnClearTable();

                if (br_apply_data.length > 0)
                    tbl.fnAddData(br_apply_data);

                setCostTextBoxes();

                $.LoadingOverlay("hide", true);
            },
            error: function (xhr, status, error) {
                showDialog("Delete fail", "<span class='error-text'>Error while processing your request. Please try again later.</span>");
                $.LoadingOverlay("hide", true);
            }
        });
    });
    //*/

    $("#add_sku_modal_button").click(function () {
        initTextBoxes();
        $("#add_sku_modal").modal("show");
    });

    $("#add_sku_button").click(function () {
        var sku = $("#sku_textbox").val();
        if (sku === null || sku === "") {
            showAlert("Please input SKU!");
            return;
        }
        var asin = $("#asin_textbox").val();
        if (asin === null || asin === "") {
            showAlert("Please input ASIN!");
            return;
        }
        var cost = $("#cost_textbox").val();
        if (cost === null || cost === "") {
            showAlert("Please input Cost!");
            return;
        }
        var qty = $("#qty_textbox").val();
        if (qty === null || qty === "") {
            showAlert("Please input Qty!");
            return;
        }
        var profit = $("#profit_textbox").val();
        if (profit === null || profit === "") {
            showAlert("Please input Profit!");
            return;
        }

        var data = [];
        data["asin"] = asin;
        data["SKU"] = sku;
        data["itemDescription"] = "";
        data["Price"] = "";
        data["recommendedQuantity"] = qty;
        data["Quantity"] = "";
        data["SQuantity"] = "";
        data["Manufacturer"] = "";
        data["ShippingWeight"] = "";
        data["DSFee"] = "";
        data["CasePack"] = "";
        data["ActualWeight"] = "";
        data["Cost"] = cost;
        data["SalesValue"] = "";
        data["SellingPrice"] = "";
        data["AmazonWeight"] = "";
        data["Profit"] = profit;
        data["AmazonWeightBasedFee"] = "";
        table.row.add(data).draw().node();
        $("#add_sku_modal").modal("hide");
    });

    showAlert = function (message) {
        alert_html = '<a href="#" class="close" data-dismiss="alert" aria-label="close">&times;</a><strong>Warning!</strong> ';
        alert_html += message;
        $(".alert").html(alert_html);
        $(".alert").show();
    }

    initTextBoxes = function () {
        $(".alert").hide();
        $("#sku_textbox").val("");
        $("#asin_textbox").val("");
        $("#cost_textbox").val("");
        $("#qty_textbox").val("");
        $("#profit_textbox").val("");
    }

});