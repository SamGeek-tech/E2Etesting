// Background script for the sample extension
console.log('Sample Extension: Background script loaded');

// Listen for messages from content scripts or popup
browser.runtime.onMessage.addListener((message, sender, sendResponse) => {
    console.log('Background received message:', message);
    
    if (message.type === 'GET_STATUS') {
        sendResponse({ status: 'active', version: '1.0.0' });
        return true;
    }
    
    if (message.type === 'STORE_DATA') {
        browser.storage.local.set({ userData: message.data })
            .then(() => sendResponse({ success: true }))
            .catch(err => sendResponse({ success: false, error: err.message }));
        return true;
    }
    
    if (message.type === 'GET_DATA') {
        browser.storage.local.get('userData')
            .then(result => sendResponse({ data: result.userData || null }))
            .catch(err => sendResponse({ error: err.message }));
        return true;
    }
    
    return false;
});

// Listen for tab updates
browser.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.status === 'complete') {
        console.log('Tab loaded:', tab.url);
    }
});

// Inject badge text to show extension is active
browser.browserAction.setBadgeText({ text: 'ON' });
browser.browserAction.setBadgeBackgroundColor({ color: '#4CAF50' });

