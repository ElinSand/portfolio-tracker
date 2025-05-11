// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
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
        document.getElementById("totalPrice").textContent = total.toFixed(2) + " USD";
    };
};



function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}




async function submitBuy() {
    const symbol = document.getElementById("buySymbol").value;
    const quantity = parseFloat(document.getElementById("buyQuantity").value);

    if (!symbol || isNaN(quantity) || quantity <= 0) {
        alert("Ange ett giltigt antal");
        return;
    }

    //const jwt = sessionStorage.getItem("JWToken");
    //if (!jwt) {
    //    alert("Ingen token hittades. Logga in igen.");
    //    return;
    //}
    const jwt = getCookie("JWToken");
    if (!jwt) {
        alert("Ingen token hittades. Logga in igen.");
        return;
    }

    try {
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
            alert(`✅ Köpet lyckades!\n${result.quantity} ${result.symbol} för ${result.totalCost} USD\nNytt saldo: ${result.newBalance}`);
            document.getElementById("buyFormContainer").style.display = "none";
        } else {
            alert(`❌ Misslyckat köp: ${result}`);
        }
    } catch (err) {
        console.error("Fel vid köp:", err);
        alert("❌ Ett fel uppstod. Försök igen.");
    }
}
