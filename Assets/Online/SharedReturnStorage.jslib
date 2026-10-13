mergeInto(LibraryManager.library, {
  DB_ReturnStorage_Get: function (keyPtr) {
    try {
      var value = localStorage.getItem("DeadBoat.Recovery.origin.v1:" + UTF8ToString(keyPtr));
      if (value === null) return 0;
      var size = lengthBytesUTF8(value) + 1;
      var ptr = _malloc(size);
      stringToUTF8(value, ptr, size);
      return ptr;
    } catch (error) {
      console.warn("[Shared recovery storage] Browser read unavailable.");
      return 0;
    }
  },
  DB_ReturnStorage_Set: function (keyPtr, valuePtr) {
    try {
      localStorage.setItem("DeadBoat.Recovery.origin.v1:" + UTF8ToString(keyPtr), UTF8ToString(valuePtr));
      return 1;
    } catch (error) {
      return 0;
    }
  }
});
