// Apollo Client Preview - iframe communication bridge

window.apolloClientPreview = {
    iframe: null,
    dotNetRef: null,
    messageHandler: null,

    initialize: function(iframeElement, dotNetReference, htmlContent) {
        this.iframe = iframeElement;
        this.dotNetRef = dotNetReference;

        if (this.messageHandler) {
            window.removeEventListener('message', this.messageHandler);
        }

        this.messageHandler = async (event) => {
            if (event.source !== this.iframe?.contentWindow) {
                return;
            }

            const data = event.data;
            if (data && data.type === 'apollo_request') {
                try {
                    const response = await this.dotNetRef.invokeMethodAsync(
                        'HandleApiRequest',
                        data.id,
                        data.method,
                        data.url,
                        data.body
                    );

                    this.iframe.contentWindow.postMessage({
                        type: 'apollo_response',
                        id: response.id,
                        status: response.status,
                        body: response.body,
                        headers: response.headers
                    }, '*');
                } catch (error) {
                    console.error('[Apollo] Error handling API request:', error);
                    
                    this.iframe.contentWindow.postMessage({
                        type: 'apollo_response',
                        id: data.id,
                        status: 500,
                        body: JSON.stringify({ error: error.message }),
                        headers: { 'Content-Type': 'application/json' }
                    }, '*');
                }
            }
        };

        window.addEventListener('message', this.messageHandler);

        // Write content to iframe using srcdoc
        this.iframe.srcdoc = htmlContent;

        console.log('[Apollo] Client preview initialized');
    },

    refresh: function(htmlContent) {
        if (this.iframe) {
            this.iframe.srcdoc = htmlContent;
        }
    },

    dispose: function() {
        if (this.messageHandler) {
            window.removeEventListener('message', this.messageHandler);
            this.messageHandler = null;
        }
        this.iframe = null;
        this.dotNetRef = null;
        console.log('[Apollo] Client preview disposed');
    }
};

