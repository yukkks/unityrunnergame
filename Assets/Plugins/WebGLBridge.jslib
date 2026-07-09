mergeInto(LibraryManager.library, {
  // Tells the parent page the game is actually interactive (the site keeps
  // its loading veil up until this arrives; iframe 'load' fires far earlier).
  NotifyGameReady: function () {
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage("game-ready", "*");
      }
    } catch (e) {}
  },

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
