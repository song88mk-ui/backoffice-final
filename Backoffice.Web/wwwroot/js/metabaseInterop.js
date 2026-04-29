window.initMetabaseDashboard = async (providerUri, instanceUrl, dashboardId, clientCodesJson) => {
    // defineMetabaseConfig must be defined before embed.js executes
    window.defineMetabaseConfig = (config) => { window.metabaseConfig = config; };

    window.defineMetabaseConfig({
        jwtProviderUri: providerUri,
        instanceUrl: instanceUrl
    });

    // Dynamically load embed.js (skip if already loaded)
    if (!document.querySelector('script[data-metabase-embed]')) {
        await new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `${instanceUrl}/app/embed.js`;
            script.setAttribute('data-metabase-embed', '1');
            script.onload = resolve;
            script.onerror = () => reject(new Error('Failed to load Metabase embed.js from ' + instanceUrl));
            document.head.appendChild(script);
        });
    }

    // Give the custom element registry a tick to register <metabase-dashboard>
    await new Promise(r => setTimeout(r, 100));

    const container = document.getElementById('metabase-container');
    if (!container) return;

    container.innerHTML = '';

    const el = document.createElement('metabase-dashboard');
    el.setAttribute('dashboard-id', String(dashboardId));
    el.setAttribute('drills', 'true');
    el.setAttribute('with-title', 'false');
    el.setAttribute('with-downloads', 'false');
    el.setAttribute('with-subscriptions', 'false');

    // Pass client codes as initial filter values.
    // Requires a "client" filter widget on the Metabase dashboard.
    const clientCodes = JSON.parse(clientCodesJson);
    if (clientCodes && clientCodes.length > 0) {
        el.setAttribute('parameters', JSON.stringify({ client: clientCodes }));
    }

    container.appendChild(el);
};
