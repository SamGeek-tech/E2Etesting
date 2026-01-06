// Popup script
document.addEventListener('DOMContentLoaded', async () => {
    const highlightBtn = document.getElementById('highlight-links');
    const pageInfoBtn = document.getElementById('get-page-info');
    const infoPanel = document.getElementById('info-panel');
    
    // Get current tab
    const tabs = await browser.tabs.query({ active: true, currentWindow: true });
    const currentTab = tabs[0];
    
    // Highlight links button
    highlightBtn.addEventListener('click', async () => {
        try {
            const response = await browser.tabs.sendMessage(currentTab.id, {
                type: 'HIGHLIGHT_LINKS'
            });
            console.log('Highlighted', response.highlighted, 'links');
            highlightBtn.textContent = `✅ Highlighted ${response.highlighted} links`;
            setTimeout(() => {
                highlightBtn.innerHTML = '<span class="btn-icon">🔗</span> Highlight Links';
            }, 2000);
        } catch (error) {
            console.error('Error highlighting links:', error);
        }
    });
    
    // Get page info button
    pageInfoBtn.addEventListener('click', async () => {
        try {
            const response = await browser.tabs.sendMessage(currentTab.id, {
                type: 'GET_PAGE_INFO'
            });
            
            document.getElementById('page-title').textContent = 
                response.title.length > 30 ? response.title.substring(0, 30) + '...' : response.title;
            document.getElementById('page-links').textContent = response.links;
            document.getElementById('page-images').textContent = response.images;
            
            infoPanel.style.display = 'block';
        } catch (error) {
            console.error('Error getting page info:', error);
        }
    });
});

