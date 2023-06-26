window.buildESport = (token, currency) => {

    //oddin.buildBifrost({
    //    token: "71BBE7D8-FD04-459C-6D8B-08D9354E5B64|7C8FCA95-FD66-4EBF-808D-22981CA2EDF0",
    //    brandToken: "1d76f102-c269-4aa6-baa9-9217b986210f",
    //    baseUrl: "https://bifrost.integration.oddin.gg",
    //    language: "EN",
    //    currency: "USD",
    //    contentElement: "#bifrost",
    //    eventHandler: (e) => {
    //        if (e.type === "REQUEST_REFRESH_BALANCE") {
    //            // call balance refresh
    //        }
    //    }
    //})

    oddin.buildBifrost({
        token: token,
        brandToken: "1d76f102-c269-4aa6-baa9-9217b986210f",
        baseUrl: "https://bifrost.integration.oddin.gg",
        language: "EN",
        currency: currency,
        contentElement: "#bifrost",
        eventHandler: (e) => {
            if (e.type === "REQUEST_REFRESH_BALANCE") {
                // call balance refresh
            }
        }
    })
}