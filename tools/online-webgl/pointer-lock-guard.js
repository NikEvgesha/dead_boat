(function (root, factory) {
  var api = factory();
  if (typeof module === "object" && module.exports) module.exports = api;
  else api.install(root.document.getElementById("unity-canvas"), root.console);
})(typeof window !== "undefined" ? window : this, function () {
  "use strict";
  var expected = ["UnknownError", "NotAllowedError", "SecurityError", "NotSupportedError", "InvalidStateError", "AbortError"];
  function install(canvas, logger) {
    if (!canvas) return false;
    var nativeRequest = canvas.requestPointerLock || canvas.mozRequestPointerLock || canvas.webkitRequestPointerLock;
    if (typeof nativeRequest !== "function") return false;
    if (nativeRequest.deadBoatGuard) return true;
    var pending = null;
    function rejected(error) {
      pending = null;
      if (!error || expected.indexOf(error.name) < 0) throw error;
      logger.warn("[Dead Boat cursor] Pointer lock rejected (" + error.name + "); cursor remains unlocked. Click the game to retry.");
    }
    function guardedRequest() {
      // Several Unity/UI transitions can request capture during the same gesture.
      if (pending) return pending;
      var result;
      try { result = nativeRequest.apply(this, arguments); }
      catch (error) { rejected(error); return; }
      if (result && typeof result.then === "function") {
        pending = result;
        // Keep the original Promise/API contract for callers with their own handler.
        // Observe expected native rejection so legacy void callers don't escalate it
        // to Unity's global fatal-error alert. Unexpected errors still propagate.
        result.then(function () { pending = null; }, rejected);
      }
      return result;
    }
    guardedRequest.deadBoatGuard = true;
    canvas.requestPointerLock = guardedRequest;
    return true;
  }
  return { install: install };
});
