window.initMetabaseDashboard = async (providerUri, instanceUrl, locale) => {
    if (!document.querySelector('script[data-metabase-embed]')) {
        window.defineMetabaseConfig = (config) => { window.metabaseConfig = config; };

        window.defineMetabaseConfig({
            jwtProviderUri: providerUri,
            instanceUrl: instanceUrl,
            locale: locale,
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

window.updateMetabaseLocale = (locale) => {
    if (!window.metabaseConfig || window.metabaseConfig.locale === locale) return;

    window.defineMetabaseConfig({ ...window.metabaseConfig, locale });

    document.querySelectorAll('metabase-dashboard').forEach((el) => {
        const parent = el.parentNode;
        const next = el.nextSibling;
        parent.removeChild(el);
        parent.insertBefore(el, next);
    });
};
