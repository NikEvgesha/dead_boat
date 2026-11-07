using System;
using DeadBoat.Online;

// Executes the actual pure readiness gate, not Unity actors or Photon transport.
internal static class EnemyAuthorityEpochProbe
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static int Main()
    {
        try
        {
            var run = new object(); var nextRun = new object(); var gate = new EnemyAuthorityEpoch();
            int canonicalHP = 40, localHP = 150, phase = 0, mask = 0, restores = 0, resumes = 0;
            bool pagesOwned = false;
            Func<bool> restore = () => {
                if (!pagesOwned) return false;
                restores++; localHP = canonicalHP; phase = 2; mask = 1;
                Require(!gate.TryPrepare(run, 1, () => throw new Exception("recursive restore"), () => {}), "no reentrant simulation during restore");
                return true;
            };
            Action resume = () => {
                Require(gate.IsReady(run, 1) && localHP == canonicalHP && phase == 2 && mask == 1, "resume only after all canonical state is installed");
                Require(gate.TryPrepare(run, 1, () => throw new Exception("resume reentry restored again"), () => {}), "resume can query authority");
                resumes++;
            };
            // Reproduce the stale-local overwrite using the previous order, without Unity.
            Require(localHP - 5 == 145 && canonicalHP - 5 == 35, "fixture exposes stale burn rollback");
            Require(!gate.TryPrepare(run, 1, restore, resume) && restores == 0 && resumes == 0, "root alone cannot resume while pages are unowned");
            pagesOwned = true;
            Require(gate.TryPrepare(run, 1, restore, resume), "delayed page ownership permits preparation");
            localHP -= 5; canonicalHP = localHP;
            Require(localHP == 35, "first burn follows canonical HP, never stale local 150");
            Require(gate.TryPrepare(run, 1, restore, resume), "subsequent sample shares the same preparation");
            localHP -= 7;
            Require(localHP == 28 && restores == 1 && resumes == 1, "ongoing damage is not rewound or coroutines restarted every frame");
            Require((mask & 1) != 0, "already hit dive victim survives preparation");
            mask |= 2;
            Require(gate.TryPrepare(run, 1, restore, resume) && mask == 3, "new dive victim is not erased by next authority query");

            gate.Invalidate(); canonicalHP = 12;
            Require(gate.TryPrepare(run, 1, restore, resume) && localHP == 12 && restores == 2, "losing and regaining authority rehydrates even with same owner ID");
            int before = restores;
            Require(gate.TryPrepare(run, 2, () => { restores++; return true; }, () => {}) && restores == before + 1, "different owner starts another epoch");
            Require(gate.TryPrepare(nextRun, 2, () => { restores++; return true; }, () => {}) && restores == before + 2, "different run with same owner cannot reuse readiness");

            gate.Invalidate(); int obsoleteResume = 0;
            Require(!gate.TryPrepare(run, 1, () => { gate.Invalidate(); return true; }, () => obsoleteResume++), "invalidation during restore cancels obsolete completion");
            Require(obsoleteResume == 0 && !gate.IsReady(run, 1), "obsolete attempt cannot resume");
            Require(!gate.TryPrepare(run, 1, () => {
                Require(!gate.TryPrepare(nextRun, 2, () => true, () => {}), "new context cannot enter an old preparation");
                return true;
            }, () => obsoleteResume++), "context replacement cancels outer preparation");
            Require(obsoleteResume == 0, "no callback from replaced run");

            foreach (bool failInResume in new[] { false, true })
            {
                gate.Invalidate(); bool threw = false;
                try { gate.TryPrepare(run, 1, () => { if (!failInResume) throw new InvalidOperationException("restore"); return true; }, () => { throw new InvalidOperationException("resume"); }); }
                catch (InvalidOperationException) { threw = true; }
                Require(threw && !gate.IsReady(run, 1), "restore/resume exception cannot leave false readiness");
                Require(gate.TryPrepare(run, 1, () => true, () => {}), "retry remains possible after failure");
            }
            Require(!gate.TryPrepare(null, 1, () => throw new Exception("null context restored"), () => {}), "missing context invalidates readiness");
            Console.WriteLine("PASS: real EnemyAuthorityEpoch; stale burn model 145 -> 35; page wait; restore-before-resume; once per epoch; ongoing damage/mask preserved; owner/run change; invalidate/reentry; exceptions/retry/null. No Unity actors, physics, coroutines, rewards, Photon or Player build.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
