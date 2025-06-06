﻿// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.




function handleBuyClick(symbol, price) {
    console.log("Clicked", symbol, price, "IsLoggedIn:", window.isLoggedIn);

    if (window.isLoggedIn) {
        document.getElementById("loginPrompt").style.display = "none";
        showBuyForm(symbol, price);
    } else {
        document.getElementById("buyFormContainer").style.display = "none";
        document.getElementById("loginPrompt").style.display = "block";
    }
}





window.showBuyForm = function (symbol, price) {
    const formCard = document.getElementById("buyFormCard");
    if (formCard) formCard.style.display = "block";

    const form = document.getElementById("buyFormContainer");
    if (!form) return;

    document.getElementById("selectedSymbol").textContent = symbol;
    document.getElementById("buySymbol").value = symbol;
    document.getElementById("buyQuantity").value = "";
    document.getElementById("totalPrice").textContent = "";

    form.style.display = "block";

    document.getElementById("buyQuantity").oninput = function () {
        const qty = parseFloat(this.value);
        const total = isNaN(qty) ? 0 : qty * parseFloat(price);
        document.getElementById("totalPrice").textContent = total.toFixed(2) + " USDT";
    };
};




function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}



//Submit Buy
//async function submitBuy() {
//    const symbol = document.getElementById("buySymbol").value;
//    const quantity = parseFloat(document.getElementById("buyQuantity").value);

//    if (!symbol || isNaN(quantity) || quantity <= 0) {
//        alert("Ange ett giltigt antal");
//        return;
//    }

//    //const jwt = sessionStorage.getItem("JWToken");
//    //if (!jwt) {
//    //    alert("Ingen token hittades. Logga in igen.");
//    //    return;
//    //}
//    const jwt = getCookie("JWToken");
//    if (!jwt) {
//        alert("Ingen token hittades. Logga in igen.");
//        return;
//    }

//    try {
//        const response = await fetch("https://localhost:7293/api/portfolio/buy", {
//            method: "POST",
//            headers: {
//                "Content-Type": "application/json",
//                "Authorization": `Bearer ${jwt}`
//            },
//            body: JSON.stringify({
//                symbol: symbol,
//                quantity: quantity
//            })
//        });

//        const result = await response.json();

//        if (response.ok) {
//            alert(`✅ Köpet lyckades!\n${result.quantity} ${result.symbol} för ${result.totalCost} USD\nNytt saldo: ${result.newBalance}`);
//            document.getElementById("buyFormContainer").style.display = "none";
//        } else {
//            alert(`❌ Misslyckat köp: ${result}`);
//        }
//    } catch (err) {
//        console.error("Fel vid köp:", err);
//        alert("❌ Ett fel uppstod. Försök igen.");
//    }
//}



//Submit Buy
async function submitBuy() {
    const symbol = document.getElementById("buySymbol").value;
    const quantity = parseFloat(document.getElementById("buyQuantity").value);

    if (!symbol || isNaN(quantity) || quantity <= 0) {
        alert("Ange ett giltigt antal");
        return;
    }

    const jwt = getCookie("JWToken");
    if (!jwt) {
        alert("Ingen token hittades. Logga in igen.");
        return;
    }

    try {
        // 1) POST mot buy-endpoint
        const response = await fetch("https://localhost:7293/api/portfolio/buy", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${jwt}`
            },
            body: JSON.stringify({
                symbol: symbol,
                quantity: quantity
            })
        });

        const result = await response.json();

        if (response.ok) {
            // Köpet lyckades – plocka ut resultatdetaljer
            const boughtQty = result.quantity;
            const boughtSymbol = result.symbol;
            const totalCost = result.totalCost; // ex. det belopp som dragits från saldot

            // 2) Hämta uppdaterat saldo från /api/portfolio/value
            const valueResponse = await fetch("https://localhost:7293/api/portfolio/value", {
                headers: {
                    "Authorization": `Bearer ${jwt}`
                }
            });

            if (!valueResponse.ok) {
                // Lyckades inte hämta saldo, men köp var ok
                alert(
                    `✅ Köpet lyckades!\n` +
                    `${boughtQty} ${boughtSymbol} för ${totalCost} USD\n` +
                    `Misslyckades att hämta uppdaterat saldo.`
                );
                document.getElementById("buyFormContainer").style.display = "none";
                return;
            }

            // Om GET /value returnerar JSON, t.ex. { "balance": 1234.56, "portfolioValue": ... }
            const valueResult = await valueResponse.json();
            const newBalance = valueResult.balance;

            alert(
                `✅ Köpet lyckades!\n` +
                `${boughtQty} ${boughtSymbol} för ${totalCost} USD\n` +
                `Nytt saldo: ${newBalance} USD`
            );
            document.getElementById("buyFormContainer").style.display = "none";
        } else {
            // Köpet misslyckades (t.ex. "Insufficient balance." eller annan validering)
            const errMsg = typeof result === "string"
                ? result
                : result.message ?? JSON.stringify(result);

            alert(`❌ Misslyckat köp: ${errMsg}`);
        }
    } catch (err) {
        console.error("Fel vid köp:", err);
        alert("❌ Ett fel uppstod. Försök igen.");
    }
}





function handleSellClick(symbol, price, ownedQuantity) {
    console.log("Säljer:", symbol, "Pris:", price, "Äger:", ownedQuantity);

    const form = document.getElementById("sellFormCard");
    if (!form) return;

    document.getElementById("sellSymbol").textContent = symbol;
    document.getElementById("sellSymbolInput").value = symbol;
    document.getElementById("sellQuantity").value = "";
    document.getElementById("sellTotalPrice").textContent = "";

    document.getElementById("sellQuantity").max = ownedQuantity;

    form.style.display = "block";

    document.getElementById("sellQuantity").oninput = function () {
        const qty = parseFloat(this.value);
        const total = isNaN(qty) ? 0 : qty * parseFloat(price);
        document.getElementById("sellTotalPrice").textContent = total.toFixed(2) + " USD";
    };
}

//Submit Sell
//async function submitSell() {
//    const symbol = document.getElementById("sellSymbolInput").value;
//    const quantity = parseFloat(document.getElementById("sellQuantity").value);

//    if (!symbol || isNaN(quantity) || quantity <= 0) {
//        alert("Ange ett giltigt antal att sälja");
//        return;
//    }

//    const jwt = getCookie("JWToken");
//    if (!jwt) {
//        alert("Ingen token hittades. Logga in igen.");
//        return;
//    }

//    try {
//        const response = await fetch("https://localhost:7293/api/portfolio/sell", {
//            method: "POST",
//            headers: {
//                "Content-Type": "application/json",
//                "Authorization": `Bearer ${jwt}`
//            },
//            body: JSON.stringify({ symbol: symbol, quantity: quantity })
//        });

//        const result = await response.json();

//        if (response.ok) {
//            alert(`✅ Försäljningen lyckades!\n${result.quantity} ${result.symbol} för ${result.totalRevenue} USD\nNytt saldo: ${result.newBalance}`);
//            document.getElementById("sellFormCard").style.display = "none";
//            window.location.reload();
//        } else {
//            alert(`❌ Misslyckad försäljning: ${result}`);
//        }
//    } catch (err) {
//        console.error("Fel vid försäljning:", err);
//        alert("❌ Ett fel uppstod. Försök igen.");
//    }
//}



//Submit Sell
async function submitSell() {
    const symbol = document.getElementById("sellSymbolInput").value;
    const quantity = parseFloat(document.getElementById("sellQuantity").value);

    if (!symbol || isNaN(quantity) || quantity <= 0) {
        alert("Ange ett giltigt antal att sälja");
        return;
    }

    const jwt = getCookie("JWToken");
    if (!jwt) {
        alert("Ingen token hittades. Logga in igen.");
        return;
    }

    try {
        // 1) POST mot sell-endpoint
        const response = await fetch("https://localhost:7293/api/portfolio/sell", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${jwt}`
            },
            body: JSON.stringify({ symbol: symbol, quantity: quantity })
        });

        // Läs ut JSON‐body (både vid OK och vid fel)
        const result = await response.json();

        if (response.ok) {
            // Försäljningen gick igenom. Visa detaljinfo om transaktionen:
            // I result kan t.ex. finnas:
            //   result.symbol, result.quantity, result.totalCost (från TransactionDto).
            const soldQty = result.quantity;
            const soldSymbol = result.symbol;
            const soldTotal = result.totalCost; // eller result.totalRevenue beroende på vad du returnerar

            // 2) Hämta uppdaterat saldo
            const valueResponse = await fetch("https://localhost:7293/api/portfolio/value", {
                headers: {
                    "Authorization": `Bearer ${jwt}`
                }
            });

            if (!valueResponse.ok) {
                // Om det gick fel att hämta saldo, visa ändå försäljningsinfo
                alert(
                    `✅ Försäljningen lyckades!\n` +
                    `${soldQty} ${soldSymbol} för ${soldTotal} USDT\n` +
                    `Men kunde inte hämta uppdaterat saldo.`
                );
                document.getElementById("sellFormCard").style.display = "none";
                return;
            }

            // Om GET /portfolio/value returnerar JSON, exempelvis:
            // { "balance": 1234.56, "portfolioValue": 7890.12, "holdings": [...] }
            const valueResult = await valueResponse.json();
            const newBalance = valueResult.balance;

            alert(
                `✅ Försäljningen lyckades!\n` +
                `${soldQty} ${soldSymbol} för ${soldTotal} USD\n` +
                `Nytt saldo: ${newBalance} USD`
            );

            document.getElementById("sellFormCard").style.display = "none";
            window.location.reload();
        } else {
            //        Fel vid försäljning (t.ex. ”Not enough shares to sell.”)
            // Ta reda på om result är en sträng eller ett objekt { message: … }
            const errMsg = typeof result === "string"
                ? result
                : result.message ?? JSON.stringify(result);

            alert(`❌ Misslyckad försäljning: ${errMsg}`);
        }
    } catch (err) {
        console.error("Fel vid försäljning:", err);
        alert("❌ Ett fel uppstod. Försök igen.");
    }
}






//Get JWT from Cookie
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}





// ===== ASSET‐LIST FILTERING (jQuery + Bootstrap d-none + No-results) =====
$(function () {
    var $input = $('#searchBox');
    var $rows = $('.asset-row');
    var $noRes = $('#noResults');

    if (!$input.length || !$rows.length) {
        console.warn('Search: saknar #searchBox eller .asset-row');
        return;
    }

    // Visa alla vid start
    $rows.removeClass('d-none');
    $noRes.hide();

    $input.on('input', function () {
        var filter = $(this).val().trim().toUpperCase();

        // Dölj ev loggning: bara logga när filter inte är tomt
        if (filter) {
            console.log('filtrerar på:', filter);
        }

        $rows.each(function () {
            var $r = $(this);
            var symbol = $r.find('strong').text().trim().toUpperCase();

            // Visa raden om symbol innehåller filter, annars dölj
            $r.toggleClass('d-none', symbol.indexOf(filter) === -1);
        });

        // Om inga syns => visa “inga resultat”
        if ($rows.filter(':visible').length === 0) {
            $noRes.show();
        } else {
            $noRes.hide();
        }
    });
});
