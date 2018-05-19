browser.tabs.onUpdated.addListener(function (tabId, changeInfo, tab) {
    if (changeInfo.url) {
        var http = new XMLHttpRequest();
        http.open("POST", "http:/localhost:9812/", true);
        http.send(changeInfo.url);
    }
});