mergeInto(LibraryManager.library, {
  // Posts the game result to the parent page (the website iframe host).
  NotifyGameResult: function (resultPtr) {
    var result = UTF8ToString(resultPtr);
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: "gameResult", result: result }, "*");
      }
      window.postMessage({ type: "gameResult", result: result }, "*");
    } catch (e) {}
  }
});
