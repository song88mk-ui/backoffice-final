window.initMetabaseDashboard = async (providerUri, instanceUrl) => {
    if (!document.querySelector('script[data-metabase-embed]')) {
        window.defineMetabaseConfig = (config) => { window.metabaseConfig = config; };

        window.defineMetabaseConfig({
            jwtProviderUri: providerUri,
            instanceUrl: instanceUrl,
        });
        await new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `${instanceUrl}/app/embed.js`;
            script.setAttribute('data-metabase-embed', '1');
            script.onload = resolve;
            script.onerror = () => reject(new Error('Failed to load embed.js from ' + instanceUrl));
            document.head.appendChild(script);
        });
    }
};
