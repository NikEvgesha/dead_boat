using UnityEngine;

namespace DeadBoat.Online
{
    // Local scene boat is a kinematic proxy; Fusion ticks own the simulation.
    public sealed class SharedBoatRuntime : MonoBehaviour
    {
        private double nextInput;
        private double nextProfile;
        private SharedBoatProfile lastProfile;
        private bool reportedProfile;
        private void LateUpdate()
        {
            var state = SharedRunContext.State;
            var board = BoardController.Instance;
            if (!SharedRunContext.Playing || state == null || board == null || !board.StartGame) return;
            if (Time.realtimeSinceStartupAsDouble >= nextProfile)
            {
                nextProfile = Time.realtimeSinceStartupAsDouble + 1;
                var profile = SharedBoatProfile.Local();
                if (!reportedProfile || !profile.Equals(lastProfile) || !state.BoatProfiles.ContainsKey(state.Runner.LocalPlayer))
                {
                    state.RPC_BoatProfile(profile);
                    reportedProfile = true;
                    lastProfile = profile;
                }
            }
            if (!state.BoatInitialized) return;
            float distance = Mathf.Lerp(board.TotalDistanceTraveled, state.BoatDistance,
                1 - Mathf.Exp(-20 * Time.unscaledDeltaTime));
            if (state.BoatFinished) distance = state.BoatDistance;
            board.ApplySharedBoat(distance, state.BoatSpeed, state.BoatFuel,
                state.BoatMaxFuel, state.BoatMaxSpeed, state.BoatFinished);
            if (state.Driver == state.Runner.LocalPlayer && Time.realtimeSinceStartupAsDouble >= nextInput)
            {
                nextInput = Time.realtimeSinceStartupAsDouble + 0.1;
                float input = PlayerInput.Instance != null && board.PlayerOnSeat && !SharedLocalGameplay.Blocked
                    ? PlayerInput.Instance.TrainMove : 0;
                state.RPC_DriverInput(input);
            }
        }
    }
}
