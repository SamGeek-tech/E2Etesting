// Content script - runs on every page
(function() {
    'use strict';
    
    console.log('Sample Extension: Content script loaded on', window.location.href);
    
    // Add a data attribute to the body to indicate extension is loaded
    document.body.setAttribute('data-sample-extension-loaded', 'true');
    
    // Create and inject the extension indicator element
    const indicator = document.createElement('div');
    indicator.id = 'sample-extension-indicator';
    indicator.className = 'sample-extension-indicator';
    indicator.innerHTML = `
        <span class="indicator-icon">🔧</span>
        <span class="indicator-text">Extension Active</span>
    `;
    document.body.appendChild(indicator);
    
    // Listen for messages from background script
    browser.runtime.onMessage.addListener((message, sender, sendResponse) => {
        if (message.type === 'PING') {
            sendResponse({ pong: true, url: window.location.href });
            return true;
        }
        
        if (message.type === 'HIGHLIGHT_LINKS') {
            const links = document.querySelectorAll('a');
            links.forEach(link => {
                link.classList.add('sample-extension-highlighted');
            });
            sendResponse({ highlighted: links.length });
            return true;
        }
        
        if (message.type === 'GET_PAGE_INFO') {
            sendResponse({
                title: document.title,
                url: window.location.href,
                links: document.querySelectorAll('a').length,
                images: document.querySelectorAll('img').length
            });
            return true;
        }
        
        return false;
    });
    
    // Expose a function on window for testing purposes (only in dev/test mode)
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        window.__sampleExtension = {
            isLoaded: true,
            version: '1.0.0',
            getIndicator: () => document.getElementById('sample-extension-indicator')
        };
    }
})();

