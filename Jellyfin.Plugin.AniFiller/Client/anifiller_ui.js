// AniFiller - Jellyfin Client-Side Script
(function () {
    console.log('AniFiller: Script loaded via Injector');

    let statusIndicator = null;

    function createStatusIndicator(status) {
        if (statusIndicator && statusIndicator.parentNode) {
            statusIndicator.parentNode.removeChild(statusIndicator);
        }

        const indicator = document.createElement('div');
        indicator.className = 'countIndicator anifiller-indicator';
        indicator.style.padding = '0px 5px';
        indicator.style.marginLeft = '5px';
        indicator.textContent = status;

        switch (status) {
            case "Manga Canon":
                indicator.style.backgroundColor = 'var(--jf-palette-success-main)';
                indicator.style.color = 'var(--jf-palette-success-contrastText)';
                break;
            case "Anime Canon":
            case "Mixed Canon/Filler":
                indicator.style.backgroundColor = 'var(--jf-palette-warning-main)';
                indicator.style.color = 'var(--jf-palette-warning-contrastText)';
                break;
            case "Filler":
                indicator.style.backgroundColor = 'var(--jf-palette-error-main)';
                indicator.style.color = 'var(--jf-palette-error-contrastText)';
                break;
            default:
                return null;
        }

        statusIndicator = indicator;
        return indicator;
    }

    function injectStatusIndicator(status) {
        console.log(`AniFiller: Injecting indicator for ${status}`);

        const titleElement = document.querySelector("p.MuiTypography-body1");
        if (titleElement) {
            const indicator = createStatusIndicator(status);
            if (!indicator) {
                console.warn('AniFiller: Unknown status, skipping indicator');
                return false;
            }

            titleElement.insertAdjacentElement('afterend', indicator);

            return true;
        }

        console.warn('AniFiller: Could not find MuiTypography-body1 element to inject indicator');
        return false;
    }

    async function checkFillerStatus(itemId) {
        console.log(`AniFiller: Checking status for ${itemId}...`);

        try {
            const response = await fetch(`/AniFiller/Episode/${itemId}`);

            if (!response.ok) {
                console.error('AniFiller: API Error', response.status);
                return;
            }

            const data = await response.json();
            console.log('AniFiller: Received data', data);

            setTimeout(() => {
                injectStatusIndicator(data.Status, data.AbsoluteEpisodeNumber);
            }, 500);

        } catch (error) {
            console.error('AniFiller: Fetch error', error);
        }
    }

    function clearFillerStatus() {
        console.log("AniFiller: Clearing filler status");
        if (statusIndicator && statusIndicator.parentNode) {
            statusIndicator.parentNode.removeChild(statusIndicator);
            statusIndicator = null;
        }
    }

    let lastCheckedId = null;

    function onPlaybackStart(id) {
        if (id && id !== lastCheckedId) {
            console.log("AniFiller: (Network) PlaybackInfo detected for ID:", id);
            lastCheckedId = id;
            checkFillerStatus(id);
        } else if (id) {
            console.log("AniFiller: (Network) PlaybackInfo detected (duplicate) for ID:", id);
        }
    }

    function onPlaybackStop() {
        console.log("AniFiller: (Network) Playback stopped");
        clearFillerStatus();
        lastCheckedId = null;
    }

    function handleUrl(url) {
        const match = url.match(/\/Items\/([a-zA-Z0-9-]+)\/PlaybackInfo/i);
        if (match) {
            onPlaybackStart(match[1]);
        } else if (url.includes("/Sessions/Playing/Stopped")) {
            onPlaybackStop();
        }
    }

    const originalFetch = window.fetch;
    window.fetch = async function (...args) {
        try {
            const url = typeof args[0] === 'string' ? args[0] : (args[0] && args[0].url);
            if (url) {
                handleUrl(url);
            }
        } catch (e) {
            console.error("AniFiller: Fetch intercept error", e);
        }
        return originalFetch.apply(this, args);
    };

    const originalXhrOpen = XMLHttpRequest.prototype.open;
    XMLHttpRequest.prototype.open = function (method, url, ...rest) {
        try {
            if (typeof url === 'string') {
                handleUrl(url);
            }
        } catch (e) {
            console.error("AniFiller: XHR intercept error", e);
        }
        return originalXhrOpen.call(this, method, url, ...rest);
    };

    console.log('AniFiller: Network interceptors attached.');

})();
