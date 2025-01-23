function bootInit() {
    const maximumRetryCount = 3;
    const retryIntervalMilliseconds = 5000;

    const startReconnectionProcess = () => {

        let isCanceled = false;

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                console.log(`Attempting to reconnect: ${i + 1} of ${maximumRetryCount}`);

                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));

                if (isCanceled) {
                    return;
                }

                try {
                    const result = await Blazor.reconnect();
                    if (!result) {
                        // The server was reached, but the connection was rejected; reload the page.
                        console.log('The server was reached, but the connection was rejected; reload the page.');
                        console.log('Boot - Reconnecting ' + result)
                        location.reload();
                        return;
                    }

                    // Successfully reconnected to the server.
                    return;
                } catch (ex){
                    // Didn't reach the server; try again.
                    console.log('Boot Error - ' + ex)
                }
            }

            // Retried too many times; reload the page.
            location.reload();
        })();

        return {
            cancel: () => {
                isCanceled = true;
            },
        };
    };

    let currentReconnectionProcess = null;

    Blazor.start({
        reconnectionHandler: {
            onConnectionDown: () => {
                console.log('Blazor is down');
                currentReconnectionProcess ??= startReconnectionProcess();
            },
            onConnectionUp: () => {
                console.log('Blazor is up');
                currentReconnectionProcess?.cancel();
                currentReconnectionProcess = null;
            }
        }
    });
};

bootInit();