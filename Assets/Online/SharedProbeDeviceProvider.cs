#if UNITY_EDITOR
using System;
using UnityEngine;
namespace DeadBoat.Online.Editor
{
    public sealed class SharedProbeDeviceProvider : DeviceProvider
    {
        public bool Visible, FailNextLock;
        public CursorLockMode Lock = CursorLockMode.Locked;
        public override bool IsInitialized() => true;
        public override bool IsMobileDevice() => false;
        public override DeviceSystemType GetSystemType() => DeviceSystemType.Windows;
        public override bool IsCursorVisible() => Visible;
        public override CursorLockMode GetCursorLockState() => Lock;
        public override void SetCursorVisible(bool visible) => Visible = visible;
        public override void SetCursorLockState(CursorLockMode state)
        {
            if (FailNextLock && state == CursorLockMode.Locked) { FailNextLock = false; throw new InvalidOperationException("injected pointer failure"); }
            Lock = state;
        }
    }
}
#endif
